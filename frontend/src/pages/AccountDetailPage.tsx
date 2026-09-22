import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { PagedTransactions, Transaction } from '../types';
import ExportTransactionsModal from '../components/ExportTransactionsModal';
import { formatCurrency } from '../utils/format';

type TypeFilter = 'All' | 'CRDT' | 'DBIT';

const PAGE_SIZE = 50;

export default function AccountDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [showExport, setShowExport] = useState(false);

  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [type, setType] = useState<TypeFilter>('All');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [refreshKey, setRefreshKey] = useState(0);

  // Pesquisa por texto com debounce — evita um pedido por tecla.
  useEffect(() => {
    const timeout = setTimeout(() => setSearch(searchInput.trim()), 350);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  // Qualquer alteração aos filtros volta à primeira página.
  useEffect(() => setPage(1), [search, type, from, to]);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    api
      .get<PagedTransactions>(`/api/bank-accounts/${id}/transactions`, {
        params: {
          page,
          pageSize: PAGE_SIZE,
          search: search || undefined,
          type: type === 'All' ? undefined : type,
          from: from || undefined,
          to: to || undefined,
        },
      })
      .then(({ data }) => {
        setTransactions(data.items);
        setTotal(data.total);
        setLoading(false);
      });
  }, [id, page, search, type, from, to, refreshKey]);

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const hasFilters = search || type !== 'All' || from || to;

  function clearFilters() {
    setSearchInput('');
    setType('All');
    setFrom('');
    setTo('');
  }

  return (
    <div>
      <Link to="/" className="lb-muted">← Voltar às contas</Link>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', margin: '12px 0 20px' }}>
        <h1 style={{ fontSize: 24 }}>Movimentos</h1>
        <button className="lb-btn" onClick={() => setShowExport(true)}>Exportar para Excel</button>
      </div>

      <div className="lb-card" style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'flex-end' }}>
        <div className="lb-field" style={{ flex: '2 1 220px', marginBottom: 0 }}>
          <label>Pesquisar</label>
          <input
            className="lb-input"
            type="text"
            placeholder="Descrição ou contraparte…"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />
        </div>
        <div className="lb-field" style={{ flex: '1 1 140px', marginBottom: 0 }}>
          <label>Tipo</label>
          <select className="lb-input" value={type} onChange={(e) => setType(e.target.value as TypeFilter)}>
            <option value="All">Todos</option>
            <option value="CRDT">Crédito</option>
            <option value="DBIT">Débito</option>
          </select>
        </div>
        <div className="lb-field" style={{ flex: '1 1 150px', marginBottom: 0 }}>
          <label>De</label>
          <input className="lb-input" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="lb-field" style={{ flex: '1 1 150px', marginBottom: 0 }}>
          <label>Até</label>
          <input className="lb-input" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        {hasFilters && (
          <button className="lb-btn-outline" onClick={clearFilters} style={{ height: 40 }}>
            Limpar filtros
          </button>
        )}
      </div>

      <p className="lb-muted" style={{ margin: '4px 0 12px' }}>
        {total === 0 ? 'Sem movimentos.' : `${total} movimento${total === 1 ? '' : 's'} encontrado${total === 1 ? '' : 's'}`}
      </p>

      {loading ? (
        <p>A carregar…</p>
      ) : (
        <table className="lb-table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Descrição</th>
              <th>Contraparte</th>
              <th style={{ textAlign: 'right' }}>Montante</th>
              <th>Estado</th>
              <th>Exportado</th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((t) => (
              <tr key={t.id}>
                <td>{t.bookingDate}</td>
                <td>{t.description || '—'}</td>
                <td>{t.counterpartyName || '—'}</td>
                <td style={{ textAlign: 'right', color: t.creditDebitIndicator === 'CRDT' ? '#0a8a2e' : '#e10a0a' }}>
                  {t.creditDebitIndicator === 'CRDT' ? '+' : '-'}{formatCurrency(t.amount, t.currency)}
                </td>
                <td>{t.status}</td>
                <td>
                  {t.isExported ? <span className="lb-badge lb-badge-success">Sim</span> : <span className="lb-muted">Não</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {!loading && total > PAGE_SIZE && (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 14, margin: '18px 0' }}>
          <button className="lb-btn-outline" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            ← Anterior
          </button>
          <span className="lb-muted">Página {page} de {totalPages}</span>
          <button className="lb-btn-outline" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
            Seguinte →
          </button>
        </div>
      )}

      {showExport && id && (
        <ExportTransactionsModal
          accountId={id}
          onClose={() => {
            setShowExport(false);
            setRefreshKey((k) => k + 1);
          }}
        />
      )}
    </div>
  );
}
