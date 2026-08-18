import { useState, type CSSProperties, type FormEvent } from 'react';
import { api } from '../api/client';

type Scope = 'All' | 'NotExported';
type Period = 'Custom' | 'PreviousMonth' | 'PreviousQuarter' | 'PreviousSemester' | 'CurrentYear';

const PERIOD_LABELS: Record<Period, string> = {
  Custom: 'Data a data',
  PreviousMonth: 'Mês anterior',
  PreviousQuarter: 'Trimestre anterior',
  PreviousSemester: 'Semestre anterior',
  CurrentYear: 'Ano atual',
};

export default function ExportTransactionsModal({ accountId, onClose }: { accountId: string; onClose: () => void }) {
  const [scope, setScope] = useState<Scope>('NotExported');
  const [period, setPeriod] = useState<Period>('CurrentYear');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (period === 'Custom' && (!from || !to)) {
      setError('Indica a data de início e de fim.');
      return;
    }

    setBusy(true);
    try {
      const response = await api.post(
        `/api/bank-accounts/${accountId}/transactions/export`,
        { scope, period, from: period === 'Custom' ? from : null, to: period === 'Custom' ? to : null },
        { responseType: 'blob' },
      );

      const disposition = response.headers['content-disposition'] as string | undefined;
      const match = disposition?.match(/filename="?([^"]+)"?/);
      const fileName = match?.[1] ?? 'movimentos.xlsx';

      const url = URL.createObjectURL(response.data);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);

      onClose();
    } catch {
      setError('Falha ao gerar o Excel. Tenta novamente.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div style={overlayStyle}>
      <div className="lb-card" style={{ maxWidth: 420, width: '100%', background: '#fff' }}>
        <h2 style={{ fontSize: 18, marginBottom: 12 }}>Exportar movimentos</h2>
        {error && <div className="lb-error-banner">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="lb-field">
            <label>Movimentos a exportar</label>
            <select className="lb-input" value={scope} onChange={(e) => setScope(e.target.value as Scope)}>
              <option value="NotExported">Só os ainda não exportados</option>
              <option value="All">Todos</option>
            </select>
          </div>

          <div className="lb-field">
            <label>Período</label>
            <select className="lb-input" value={period} onChange={(e) => setPeriod(e.target.value as Period)}>
              {(Object.keys(PERIOD_LABELS) as Period[]).map((p) => (
                <option key={p} value={p}>{PERIOD_LABELS[p]}</option>
              ))}
            </select>
          </div>

          {period === 'Custom' && (
            <div style={{ display: 'flex', gap: 12 }}>
              <div className="lb-field" style={{ flex: 1 }}>
                <label>De</label>
                <input className="lb-input" type="date" required value={from} onChange={(e) => setFrom(e.target.value)} />
              </div>
              <div className="lb-field" style={{ flex: 1 }}>
                <label>Até</label>
                <input className="lb-input" type="date" required value={to} onChange={(e) => setTo(e.target.value)} />
              </div>
            </div>
          )}

          <div style={{ display: 'flex', gap: 8, marginTop: 8 }}>
            <button className="lb-btn" type="submit" disabled={busy}>
              {busy ? 'A gerar…' : 'Exportar para Excel'}
            </button>
            <button className="lb-btn-outline" type="button" onClick={onClose} disabled={busy}>
              Cancelar
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

const overlayStyle: CSSProperties = {
  position: 'fixed',
  inset: 0,
  background: 'rgba(0,0,0,0.35)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: 1000,
  padding: 16,
};
