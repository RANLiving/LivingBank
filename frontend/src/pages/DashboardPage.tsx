import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { BankAccount, Company, SyncStatus } from '../types';
import { useAuth } from '../context/AuthContext';
import CompanyAssignModal from '../components/CompanyAssignModal';

function formatCurrency(amount: number, currency: string) {
  return new Intl.NumberFormat('pt-PT', { style: 'currency', currency }).format(amount);
}

const COLLAPSE_KEY = 'lb_collapsed_company_groups';

function loadCollapsedState(): Record<string, boolean> {
  try {
    return JSON.parse(localStorage.getItem(COLLAPSE_KEY) ?? '{}');
  } catch {
    return {};
  }
}

interface Group {
  key: string;
  name: string;
  accounts: BankAccount[];
}

export default function DashboardPage() {
  const { hasRole } = useAuth();
  const [accounts, setAccounts] = useState<BankAccount[]>([]);
  const [companies, setCompanies] = useState<Company[]>([]);
  const [statuses, setStatuses] = useState<Record<string, SyncStatus>>({});
  const [syncingId, setSyncingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>(loadCollapsedState);
  const [assigningAccount, setAssigningAccount] = useState<BankAccount | null>(null);

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

  const groups = useMemo<Group[]>(() => {
    const byCompany = new Map<string, Group>();
    for (const a of accounts) {
      const key = a.companyId ?? '__none__';
      const name = a.companyName ?? 'Sem empresa';
      if (!byCompany.has(key)) byCompany.set(key, { key, name, accounts: [] });
      byCompany.get(key)!.accounts.push(a);
    }
    const list = [...byCompany.values()].sort((a, b) => a.name.localeCompare(b.name, 'pt'));
    const withoutCompany = list.filter((g) => g.key === '__none__');
    const withCompany = list.filter((g) => g.key !== '__none__');
    return [...withoutCompany, ...withCompany];
  }, [accounts]);

  function toggleGroup(key: string) {
    setCollapsed((prev) => {
      const next = { ...prev, [key]: !prev[key] };
      localStorage.setItem(COLLAPSE_KEY, JSON.stringify(next));
      return next;
    });
  }

  function groupTotals(group: Group) {
    const byCurrency = new Map<string, number>();
    for (const a of group.accounts) {
      if (a.latestBalance === null) continue;
      byCurrency.set(a.currency, (byCurrency.get(a.currency) ?? 0) + a.latestBalance);
    }
    return [...byCurrency.entries()].map(([currency, amount]) => formatCurrency(amount, currency)).join(' + ');
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

      {groups.map((group) => {
        const isCollapsed = collapsed[group.key];
        const totals = groupTotals(group);
        return (
          <div key={group.key} style={{ marginBottom: 20 }}>
            <button
              onClick={() => toggleGroup(group.key)}
              style={{
                display: 'flex', alignItems: 'center', gap: 8, width: '100%',
                background: 'none', border: 'none', borderBottom: '1px solid #f2c9c9',
                padding: '8px 0', cursor: 'pointer', textAlign: 'left',
              }}
            >
              <span style={{ fontSize: 12, transform: isCollapsed ? 'rotate(-90deg)' : 'none', transition: 'transform 0.15s' }}>▾</span>
              <strong style={{ fontSize: 16 }}>{group.name}</strong>
              <span className="lb-muted">
                ({group.accounts.length} {group.accounts.length === 1 ? 'conta' : 'contas'}{totals ? ` · ${totals}` : ''})
              </span>
            </button>

            {!isCollapsed && (
              <div className="lb-grid" style={{ marginTop: 12 }}>
                {group.accounts.map((a) => {
                  const status = statuses[a.id];
                  return (
                    <div className="lb-card" key={a.id} style={{ position: 'relative' }}>
                      {hasRole('Admin', 'Manager') && (
                        <button
                          onClick={() => setAssigningAccount(a)}
                          title="Mudar empresa"
                          style={{
                            position: 'absolute', top: 10, right: 10, background: 'none', border: 'none',
                            cursor: 'pointer', fontSize: 14, color: 'var(--lb-text-muted)',
                          }}
                        >
                          ✏️
                        </button>
                      )}
                      <div style={{ display: 'flex', justifyContent: 'space-between', paddingRight: 20 }}>
                        <strong>{a.displayName}</strong>
                        {!a.isActive && <span className="lb-badge lb-badge-error">Inativa</span>}
                      </div>
                      <div className="lb-muted">{a.bankName} · {a.iban}</div>
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
            )}
          </div>
        );
      })}

      {accounts.length === 0 && <p className="lb-muted">Nenhuma conta bancária configurada.</p>}

      {assigningAccount && (
        <CompanyAssignModal
          account={assigningAccount}
          companies={companies}
          onClose={() => setAssigningAccount(null)}
          onSaved={load}
        />
      )}
    </div>
  );
}
