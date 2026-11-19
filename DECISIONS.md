\# Decisiones, supuestos y trade-offs



\## Decisiones técnicas



\### Idempotencia en la creación

\*\*Problema:\*\* los usuarios reintentan cuando ven lentitud, y no se deben crear dos solicitudes para la misma operación.

\*\*Alternativas:\*\* validar solo en código, o validar en código más una restricción en la base de datos.

\*\*Decisión:\*\* índice único en `ExternalReference`. El servicio primero busca si existe; si dos peticiones llegan al mismo tiempo, la base de datos deja pasar solo una y la otra captura el `DbUpdateException` y devuelve la existente.

\*\*Validación:\*\* prueba automatizada y prueba manual en Swagger (201 y luego 200 con el mismo id).



\### Evitar doble procesamiento en el worker

\*\*Problema:\*\* si hay más de una instancia del worker, dos podrían tomar la misma solicitud y llamar dos veces al proveedor.

\*\*Decisión:\*\* `Status` es token de concurrencia. El paso de `Pending` a `Processing` solo se guarda si el estado sigue siendo `Pending`; si otro proceso la tomó primero, sale `DbUpdateConcurrencyException` y se salta.

\*\*Alternativa descartada:\*\* bloqueo pesimista; SQLite no lo soporta y agrega complejidad.



\### Idempotencia ante el proveedor

La `externalReference` se envía como clave de idempotencia. El proveedor simulado recuerda las referencias procesadas y no repite el efecto. Con un proveedor real iría como header `Idempotency-Key`.



\### Reintentos con Polly

Hasta 3 reintentos con espera exponencial (1 s, 2 s, 4 s) y jitter, solo para `TransientProviderException`. Si se agotan, la solicitud pasa a `Failed` y se guarda el error en `LastError`. El pipeline se inyecta, así que en las pruebas se usa uno sin esperas.



\### Reglas de estado en el dominio

Las transiciones viven en la entidad (`StartProcessing`, `Complete`, `Fail`) con setters privados. Permitidas: `Pending → Processing`, `Processing → Completed` y `Processing → Failed`. `Completed` no puede volver a ningún estado.



\### Persistencia: SQLite

Elegida porque no requiere instalar nada y los datos sobreviven al proceso. Las migraciones se aplican al arrancar.



\*\*Si se usara NoSQL\*\* (por ejemplo MongoDB): `externalReference` sería un índice único en la colección o directamente el `\_id` del documento. El cambio de estado se haría con un `findOneAndUpdate` filtrando por `status: "Pending"`, que cumple el mismo papel del token de concurrencia. Se perderían las migraciones de EF y la consulta por estado necesitaría su propio índice.



\### Estructura

Un proyecto `Core` compartido por la API y el worker, para no duplicar entidad, reglas ni acceso a datos. No agregué más capas para mantener el alcance acorde a la prueba.



\### Frontend

React con Vite. La lista se refresca cada 3 segundos (polling) para ver el cambio de estado. SignalR daría actualización en tiempo real, pero es más complejo para este alcance. La URL de la API se lee de variables de entorno.



\## Supuestos



\- \*\*Misma referencia con datos diferentes:\*\* la prueba no lo define. Asumí que es un error del cliente y se responde `409 Conflict`, sin modificar la solicitud original.

\- \*\*Campos extra:\*\* agregué `Attempts`, `LastError`, `CreatedAt` y `UpdatedAt` para trazabilidad y para mostrar el error en pantalla.

\- \*\*Fallo definitivo:\*\* después de agotar los reintentos la solicitud queda en `Failed` y no se reintenta sola. Reprocesarla sería una acción manual que no implementé.

\- \*\*Sin autenticación\*\*, como indica la prueba. Las entradas se validan con DataAnnotations y los errores no exponen detalles internos (`ProblemDetails`).



\## Riesgos y pendientes



\- \*\*Solicitudes atascadas en `Processing`:\*\* si el worker se cae a mitad de un proceso, la solicitud queda en ese estado. Siguiente paso: guardar la fecha en que se tomó y regresarla a `Pending` si supera un tiempo límite.

\- \*\*Memoria del proveedor simulado en RAM:\*\* se pierde al reiniciar el worker. Es solo para la simulación; un proveedor real persiste sus claves de idempotencia.

\- \*\*HTTPS:\*\* quité la redirección a HTTPS para facilitar las pruebas locales con el frontend. En producción va activa.

\- \*\*Pruebas que agregaría:\*\* conflicto por datos diferentes, fallo transitorio seguido de éxito, y pruebas de integración de la API con `WebApplicationFactory`.



\## Revisión de código legado



\### Fragmento A – WebForms

\*\*Riesgo:\*\* inyección SQL. El valor de `txtCustomer.Text` se concatena directo en la consulta; alguien podría escribir `' OR 1=1 --` y ver o modificar datos. Además, el `SqlDataReader` queda abierto mientras se hace el binding.

\*\*Cambio mínimo:\*\* usar parámetros.

```csharp

var sql = "SELECT Id, Customer, Status FROM ServiceRequests WHERE Customer = @customer";

using var cn = new SqlConnection(ConfigurationManager.ConnectionStrings\["Main"].ConnectionString);

using var cmd = new SqlCommand(sql, cn);

cmd.Parameters.Add("@customer", SqlDbType.NVarChar, 100).Value = txtCustomer.Text.Trim();

cn.Open();

using var reader = cmd.ExecuteReader();

GridView1.DataSource = reader;

GridView1.DataBind();

```



\### Fragmento B – ASP.NET MVC

\*\*Riesgos:\*\*

\- Acepta cualquier texto como estado, sin validar, así que una solicitud `Completed` puede volver a `Pending`, o quedar con un estado que no existe.

\- Si `Find(id)` no encuentra nada, lanza `NullReferenceException` (500).

\- Le falta protección CSRF en un POST que modifica datos.



\*\*Cambio mínimo:\*\*

```csharp

\[HttpPost]

\[ValidateAntiForgeryToken]

public ActionResult UpdateStatus(int id, string status)

{

&#x20;   if (!Enum.TryParse<RequestStatus>(status, true, out var newStatus))

&#x20;   {

&#x20;       return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Estado inválido");

&#x20;   }



&#x20;   var request = db.ServiceRequests.Find(id);

&#x20;   if (request == null)

&#x20;   {

&#x20;       return HttpNotFound();

&#x20;   }



&#x20;   if (request.Status == RequestStatus.Completed)

&#x20;   {

&#x20;       return new HttpStatusCodeResult(HttpStatusCode.Conflict, "Una solicitud completada no puede cambiar de estado");

&#x20;   }



&#x20;   request.Status = newStatus;

&#x20;   db.SaveChanges();

&#x20;   return Json(new { request.Id, request.Status });

}

```

Además, devuelvo un objeto reducido en vez de la entidad completa, para no exponer campos internos.



\## Nota de uso de IA



Utilicé Claude como herramienta de apoyo durante el desarrollo. Lo usé principalmente para:



\- \*\*Evaluar alternativas de diseño:\*\* comparar opciones para separar la API del worker, implementar la idempotencia y controlar la concurrencia cuando hay varias instancias del worker.

\- \*\*Apoyar la implementación:\*\* generar borradores iniciales de algunas partes del código y de las pruebas automatizadas, que posteriormente revisé y adapté al diseño del proyecto.

\- \*\*Documentación:\*\* ayudar a organizar y redactar las decisiones técnicas, los supuestos y los riesgos identificados.



\*\*Cómo verifiqué los resultados:\*\*



No tomé las respuestas de la IA como soluciones definitivas. Revisé el código generado y comprobé su comportamiento mediante:



\- Pruebas automatizadas, especialmente para los escenarios de idempotencia y concurrencia.

\- Pruebas manuales en Swagger: creación de solicitudes (201), reintento con la misma referencia (200 con el mismo ID), misma referencia con datos diferentes (409) e identificador inexistente (404).

\- Ejecución conjunta de la API, el worker y el frontend.

\- Simulación de fallos del proveedor (`FailureRate = 1.0`) para comprobar que, después de agotar los reintentos, la solicitud pasara a `Failed`.



\*\*Qué corregí o ajusté durante el desarrollo:\*\*



\- Corregí las versiones de algunos paquetes NuGet, porque inicialmente se instalaban versiones incompatibles con .NET 9.

\- Agregué un `nuget.config` local para evitar problemas de restauración causados por un feed privado configurado en mi máquina.

\- Incorporé Swagger UI para facilitar las pruebas manuales de la API.

\- Agregué `web/.env.example` para documentar la configuración necesaria del frontend sin subir el archivo `.env`.

\- Ajusté el código generado para mantener las reglas de negocio en la entidad, controlar la concurrencia mediante el estado y configurar los reintentos únicamente para errores transitorios.



La implementación final, las decisiones técnicas y las pruebas ejecutadas fueron revisadas por mí. Utilicé la IA como apoyo, no como sustituto de la comprensión y validación del código.

