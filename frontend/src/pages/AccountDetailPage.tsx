import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { Transaction } from '../types';
import ExportTransactionsModal from '../components/ExportTransactionsModal';

function formatCurrency(amount: number, currency: string) {
  return new Intl.NumberFormat('pt-PT', { style: 'currency', currency }).format(amount);
}

export default function AccountDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [showExport, setShowExport] = useState(false);

  function load() {
    if (!id) return;
    setLoading(true);
    api.get<Transaction[]>(`/api/bank-accounts/${id}/transactions`).then(({ data }) => {
      setTransactions(data);
      setLoading(false);
    });
  }

  useEffect(load, [id]);

  return (
    <div>
      <Link to="/" className="lb-muted">← Voltar às contas</Link>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', margin: '12px 0 20px' }}>
        <h1 style={{ fontSize: 24 }}>Movimentos</h1>
        <button className="lb-btn" onClick={() => setShowExport(true)}>Exportar para Excel</button>
      </div>

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
      {!loading && transactions.length === 0 && <p className="lb-muted">Sem movimentos.</p>}

      {showExport && id && (
        <ExportTransactionsModal
          accountId={id}
          onClose={() => {
            setShowExport(false);
            load();
          }}
        />
      )}
    </div>
  );
}
