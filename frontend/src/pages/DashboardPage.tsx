import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { BankAccount, Company, SyncStatus } from '../types';
import { useAuth } from '../context/AuthContext';

function formatCurrency(amount: number, currency: string) {
  return new Intl.NumberFormat('pt-PT', { style: 'currency', currency }).format(amount);
}

export default function DashboardPage() {
  const { hasRole } = useAuth();
  const [accounts, setAccounts] = useState<BankAccount[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [statuses, setStatuses] = useState<Record<string, SyncStatus>>({});
  const [syncingId, setSyncingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    const { data } = await api.get<BankAccount[]>('/api/bank-accounts');
    setAccounts(data);
    const entries = await Promise.all(
      data.map(async (a) => {
        const { data: s } = await api.get<SyncStatus>(`/api/sync/status/${a.id}`);
        return [a.id, s] as const;
      }),
    );
    setStatuses(Object.fromEntries(entries));
  }

  useEffect(() => {
    load();
    api.get<Company[]>('/api/companies').then(({ data }) => setCompanies(data.filter((c) => c.isActive)));
  }, []);

  async function handleAssignCompany(accountId: string, companyId: string) {
    await api.patch(`/api/bank-accounts/${accountId}/company`, { companyId: companyId || null });
    await load();
  }

  async function handleForceSync(accountId: string) {
    setError(null);
    setSyncingId(accountId);
    try {
      await api.post(`/api/sync/force/${accountId}`);
      await load();
    } catch (err: any) {
      setError(err?.response?.data?.error ?? 'Falha ao forçar leitura.');
    } finally {
      setSyncingId(null);
    }
  }

  return (
    <div>
      <h1 style={{ fontSize: 24, marginBottom: 16 }}>Contas bancárias</h1>
      {error && <div className="lb-error-banner">{error}</div>}

      <div className="lb-grid">
        {accounts.map((a) => {
          const status = statuses[a.id];
          return (
            <div className="lb-card" key={a.id}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <strong>{a.displayName}</strong>
                {!a.isActive && <span className="lb-badge lb-badge-error">Inativa</span>}
              </div>
              <div className="lb-muted">{a.bankName} · {a.iban}</div>
              {hasRole('Admin', 'Manager') ? (
                <select
                  className="lb-input"
                  style={{ marginTop: 6, fontSize: 13, padding: '4px 8px' }}
                  value={a.companyId ?? ''}
                  onChange={(e) => handleAssignCompany(a.id, e.target.value)}
                >
                  <option value="">Sem empresa</option>
                  {companies.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              ) : (
                a.companyName && <div className="lb-muted">Empresa: {a.companyName}</div>
              )}
              <div style={{ fontSize: 28, fontWeight: 700, margin: '12px 0' }}>
                {a.latestBalance !== null ? formatCurrency(a.latestBalance, a.currency) : '—'}
              </div>
              <div className="lb-muted" style={{ marginBottom: 4 }}>
                {a.transactionCount} {a.transactionCount === 1 ? 'movimento' : 'movimentos'}
              </div>
              {status && (
                <div className="lb-muted" style={{ marginBottom: 10 }}>
                  Leituras hoje: {status.todayCount}/{status.maxDaily} ({status.remaining} restantes)
                </div>
              )}
              <div style={{ display: 'flex', gap: 8 }}>
                <Link to={`/accounts/${a.id}`} className="lb-btn-outline">Ver movimentos</Link>
                {hasRole('Admin', 'Manager') && (
                  <button
                    className="lb-btn"
                    disabled={syncingId === a.id || (status && status.remaining <= 0)}
                    onClick={() => handleForceSync(a.id)}
                  >
                    {syncingId === a.id ? 'A ler…' : 'Forçar leitura'}
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {accounts.length === 0 && <p className="lb-muted">Nenhuma conta bancária configurada.</p>}
    </div>
  );
}
