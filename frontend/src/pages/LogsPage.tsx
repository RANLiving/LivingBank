import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { AuditLogEntry, ErrorLogEntry } from '../types';

type Tab = 'errors' | 'audit';

export default function LogsPage() {
  const [tab, setTab] = useState<Tab>('errors');
  const [errors, setErrors] = useState<ErrorLogEntry[]>([]);
  const [audit, setAudit] = useState<AuditLogEntry[]>([]);

  useEffect(() => {
    if (tab === 'errors') {
      api.get('/api/logs/errors').then(({ data }) => setErrors(data.items));
    } else {
      api.get('/api/logs/audit').then(({ data }) => setAudit(data.items));
    }
  }, [tab]);

  async function resolveError(id: number) {
    await api.patch(`/api/logs/errors/${id}/resolve`);
    setErrors((prev) => prev.map((e) => (e.id === id ? { ...e, resolved: true } : e)));
  }

  return (
    <div>
      <h1 style={{ fontSize: 24, marginBottom: 16 }}>Logs da plataforma</h1>

      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        <button className={tab === 'errors' ? 'lb-btn' : 'lb-btn-outline'} onClick={() => setTab('errors')}>Ecrã de erros</button>
        <button className={tab === 'audit' ? 'lb-btn' : 'lb-btn-outline'} onClick={() => setTab('audit')}>Auditoria</button>
      </div>

      {tab === 'errors' && (
        <table className="lb-table">
          <thead>
            <tr>
              <th>Quando</th>
              <th>Origem</th>
              <th>Mensagem</th>
              <th>Caminho</th>
              <th>Estado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {errors.map((e) => (
              <tr key={e.id}>
                <td>{new Date(e.timestamp).toLocaleString('pt-PT')}</td>
                <td>{e.source}</td>
                <td>{e.message}</td>
                <td className="lb-muted">{e.path}</td>
                <td>
                  <span className={`lb-badge ${e.resolved ? 'lb-badge-success' : 'lb-badge-error'}`}>
                    {e.resolved ? 'Resolvido' : 'Pendente'}
                  </span>
                </td>
                <td>
                  {!e.resolved && (
                    <button className="lb-btn-outline" onClick={() => resolveError(e.id)}>Marcar resolvido</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {tab === 'audit' && (
        <table className="lb-table">
          <thead>
            <tr>
              <th>Quando</th>
              <th>Utilizador</th>
              <th>Ação</th>
              <th>Estado HTTP</th>
              <th>IP</th>
            </tr>
          </thead>
          <tbody>
            {audit.map((a) => (
              <tr key={a.id}>
                <td>{new Date(a.timestamp).toLocaleString('pt-PT')}</td>
                <td>{a.userName || '—'}</td>
                <td>{a.action}</td>
                <td>{a.statusCode}</td>
                <td className="lb-muted">{a.ipAddress}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
