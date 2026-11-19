# Portal de solicitudes

Solución para registrar solicitudes de servicio y procesarlas en segundo plano contra un proveedor externo simulado.

## Estructura

- `src/Solicitudes.Core`: dominio, persistencia (EF Core + SQLite) y lógica de procesamiento.
- `src/Solicitudes.Api`: API REST en ASP.NET Core (.NET 9).
- `src/Solicitudes.Worker`: proceso en segundo plano (Worker Service), preparado para correr como Windows Service.
- `tests/Solicitudes.Tests`: pruebas automatizadas con xUnit.
- `web`: frontend en React (Vite).

## Requisitos

- .NET SDK 9
- Node.js 20 o superior

## Cómo ejecutarlo

### 1. Base de datos

No hay que hacer nada manual. La API y el worker aplican las migraciones al arrancar y crean `solicitudes.db` (SQLite) en la raíz del repositorio.

### 2. API

```bash
dotnet run --project src/Solicitudes.Api
```

Queda en `http://localhost:5150`. Swagger: `http://localhost:5150/swagger`.
También está `src/Solicitudes.Api/Solicitudes.Api.http` con peticiones de ejemplo, incluido el caso de reintento.

### 3. Worker

En otra terminal:

```bash
dotnet run --project src/Solicitudes.Worker
```

Revisa cada 5 segundos si hay solicitudes `Pending` y las procesa. La tasa de fallos del proveedor simulado se configura en `src/Solicitudes.Worker/appsettings.json` (`Provider:FailureRate`).

En Visual Studio se pueden arrancar los dos juntos: clic derecho en la solución → Configurar proyectos de inicio → Varios proyectos de inicio.

### 4. Frontend

```bash
cd web
npm install
npm run dev
```

Abrir `http://localhost:5173`. La URL de la API se configura en `web/.env` (ver `web/.env.example`). Si no existe, usa `http://localhost:5150`.

### 5. Pruebas

```bash
dotnet test
```

Cubren:
- Una solicitud `Completed` no puede volver a otro estado.
- Un reintento con la misma `externalReference` no crea otra solicitud.
- Si el proveedor falla en todos los intentos, la solicitud queda en `Failed` con el error guardado.
- Una solicitud ya procesada no vuelve a llamar al proveedor.

Usan SQLite en memoria, así que validan el comportamiento real del índice único y del control de concurrencia.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/requests` | Crea una solicitud. `201` si es nueva, `200` si ya existía con los mismos datos, `409` si la referencia existe con datos diferentes, `400` si la entrada es inválida. |
| GET | `/api/requests?status=Pending` | Lista solicitudes. Sin `status` devuelve todas. |
| GET | `/api/requests/{id}` | Consulta una solicitud. `404` si no existe. |

## Llevar el worker a Windows Service

El worker ya usa `AddWindowsService()`, así que no hay que cambiar código. Se publica y se registra en Windows:

```
dotnet publish src/Solicitudes.Worker -c Release -o C:\Services\SolicitudesWorker
sc.exe create SolicitudesWorker binPath= "C:\Services\SolicitudesWorker\Solicitudes.Worker.exe" start= auto
sc.exe start SolicitudesWorker
```

Así arranca solo con el servidor y corre en segundo plano sin una consola abierta.

Como servicio, el directorio de trabajo cambia, así que la cadena de conexión debe ir con ruta absoluta, por variable de entorno (`ConnectionStrings__Default`) o en el `appsettings.json` publicado. En un entorno real apuntaría a SQL Server y los secretos irían en variables de entorno o en un gestor de secretos, no en el repositorio.

Más detalle de las decisiones en [DECISIONS.md](DECISIONS.md).