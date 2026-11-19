import { useEffect, useState } from 'react';
import './App.css';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5150';
const STATUSES = ['Pending', 'Processing', 'Completed', 'Failed'];
const EMPTY_FORM = { externalReference: '', customerId: '', serviceCode: '', description: '' };

function App() {
  const [requests, setRequests] = useState([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [form, setForm] = useState(EMPTY_FORM);
  const [message, setMessage] = useState(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadRequests();
    const interval = setInterval(loadRequests, 3000);
    return () => clearInterval(interval);
  }, [statusFilter]);

  async function loadRequests() {
    const query = statusFilter ? `?status=${statusFilter}` : '';

    try {
      const response = await fetch(`${API_URL}/api/requests${query}`);
      if (!response.ok) {
        throw new Error(`Error ${response.status}`);
      }
      setRequests(await response.json());
    } catch {
      setMessage({ type: 'error', text: 'No se pudo conectar con la API.' });
    }
  }

  function handleChange(event) {
    setForm({ ...form, [event.target.name]: event.target.value });
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    setMessage(null);

    try {
      const response = await fetch(`${API_URL}/api/requests`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form),
      });
      const body = await response.json();

      if (response.status === 201) {
        setMessage({ type: 'success', text: `Solicitud ${body.externalReference} creada.` });
        setForm(EMPTY_FORM);
      } else if (response.status === 200) {
        setMessage({ type: 'info', text: `La solicitud ${body.externalReference} ya existía, no se creó otra.` });
      } else {
        setMessage({ type: 'error', text: getErrorMessage(body) });
      }

      loadRequests();
    } catch {
      setMessage({ type: 'error', text: 'No se pudo conectar con la API.' });
    } finally {
      setSaving(false);
    }
  }

  function getErrorMessage(body) {
    if (body.errors) {
      return Object.values(body.errors).flat().join(' ');
    }
    return body.title ?? 'Ocurrió un error.';
  }

  return (
    <main className="container">
      <h1>Portal de solicitudes</h1>

      <section className="card">
        <h2>Nueva solicitud</h2>
        <form onSubmit={handleSubmit} className="form">
          <input name="externalReference" placeholder="Referencia externa" value={form.externalReference} onChange={handleChange} required maxLength={100} />
          <input name="customerId" placeholder="Cliente" value={form.customerId} onChange={handleChange} required maxLength={50} />
          <input name="serviceCode" placeholder="Código de servicio" value={form.serviceCode} onChange={handleChange} required maxLength={50} />
          <input name="description" placeholder="Descripción" value={form.description} onChange={handleChange} required maxLength={500} />
          <button type="submit" disabled={saving}>{saving ? 'Enviando...' : 'Crear solicitud'}</button>
        </form>
        {message && <p className={`message ${message.type}`}>{message.text}</p>}
      </section>

      <section className="card">
        <div className="list-header">
          <h2>Solicitudes</h2>
          <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
            <option value="">Todos los estados</option>
            {STATUSES.map((status) => (
              <option key={status} value={status}>{status}</option>
            ))}
          </select>
        </div>

        <table>
          <thead>
            <tr>
              <th>Referencia</th>
              <th>Cliente</th>
              <th>Servicio</th>
              <th>Estado</th>
              <th>Intentos</th>
              <th>Error</th>
            </tr>
          </thead>
          <tbody>
            {requests.length === 0 && (
              <tr>
                <td colSpan={6} className="empty">No hay solicitudes.</td>
              </tr>
            )}
            {requests.map((request) => (
              <tr key={request.id}>
                <td>{request.externalReference}</td>
                <td>{request.customerId}</td>
                <td>{request.serviceCode}</td>
                <td><span className={`status ${request.status.toLowerCase()}`}>{request.status}</span></td>
                <td>{request.attempts}</td>
                <td>{request.lastError ?? '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </main>
  );
}

export default App;