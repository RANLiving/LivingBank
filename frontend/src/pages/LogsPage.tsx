import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { AuditLogEntry, BankAccount, ErrorLogEntry, SyncLog } from '../types';

type Tab = 'errors' | 'audit' | 'sync';

export default function LogsPage() {
  const [tab, setTab] = useState<Tab>('errors');
  const [errors, setErrors] = useState<ErrorLogEntry[]>([]);
  const [audit, setAudit] = useState<AuditLogEntry[]>([]);
  const [syncLogs, setSyncLogs] = useState<SyncLog[]>([]);
  const [accounts, setAccounts] = useState<Record<string, string>>({});

  useEffect(() => {
    api.get<BankAccount[]>('/api/bank-accounts').then(({ data }) => {
      setAccounts(Object.fromEntries(data.map((a) => [a.id, a.displayName])));
    });
  }, []);

  useEffect(() => {
    if (tab === 'errors') {
      api.get('/api/logs/errors').then(({ data }) => setErrors(data.items));
    } else if (tab === 'audit') {
      api.get('/api/logs/audit').then(({ data }) => setAudit(data.items));
    } else {
      api.get<SyncLog[]>('/api/sync/logs?take=100').then(({ data }) => setSyncLogs(data));
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
        <button className={tab === 'sync' ? 'lb-btn' : 'lb-btn-outline'} onClick={() => setTab('sync')}>Sincronizações (Enable Banking)</button>
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

      {tab === 'sync' && (
        <table className="lb-table">
          <thead>
            <tr>
              <th>Quando</th>
              <th>Conta</th>
              <th>Origem</th>
              <th>Estado</th>
              <th>Saldos</th>
              <th>Movimentos</th>
              <th>Erro</th>
            </tr>
          </thead>
          <tbody>
            {syncLogs.map((s) => (
              <tr key={s.id}>
                <td>{new Date(s.startedAt).toLocaleString('pt-PT')}</td>
                <td>{accounts[s.bankAccountId] ?? s.bankAccountId}</td>
                <td>{s.syncTrigger === 'Scheduled' ? 'Agendada' : s.syncTrigger === 'Manual' ? 'Manual' : s.syncTrigger}</td>
                <td>
                  <span className={`lb-badge ${s.status === 'Success' ? 'lb-badge-success' : 'lb-badge-error'}`}>
                    {s.status === 'Success' ? 'Sucesso' : 'Falha'}
                  </span>
                </td>
                <td>{s.balancesFetched}</td>
                <td>{s.transactionsFetched}</td>
                <td className="lb-muted">{s.errorMessage || '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {tab === 'sync' && syncLogs.length === 0 && <p className="lb-muted">Sem sincronizações registadas.</p>}
    </div>
  );
}
