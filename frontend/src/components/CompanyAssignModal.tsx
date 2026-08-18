import { useState, type CSSProperties, type FormEvent } from 'react';
import { api } from '../api/client';
import type { BankAccount, Company } from '../types';

export default function CompanyAssignModal({
  account,
  companies,
  onClose,
  onSaved,
}: {
  account: BankAccount;
  companies: Company[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const [companyId, setCompanyId] = useState(account.companyId ?? '');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await api.patch(`/api/bank-accounts/${account.id}/company`, { companyId: companyId || null });
      onSaved();
      onClose();
    } catch {
      setError('Falha ao mudar a empresa. Tenta novamente.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div style={overlayStyle}>
      <div className="lb-card" style={{ maxWidth: 360, width: '100%', background: '#fff' }}>
        <h2 style={{ fontSize: 16, marginBottom: 4 }}>Mudar empresa</h2>
        <p className="lb-muted" style={{ marginBottom: 12 }}>{account.displayName} · {account.iban}</p>

        {error && <div className="lb-error-banner">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="lb-field">
            <label>Empresa</label>
            <select className="lb-input" value={companyId} onChange={(e) => setCompanyId(e.target.value)}>
              <option value="">Sem empresa</option>
              {companies.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>

          <div style={{ display: 'flex', gap: 8 }}>
            <button className="lb-btn" type="submit" disabled={busy}>{busy ? 'A guardar…' : 'Guardar'}</button>
            <button className="lb-btn-outline" type="button" onClick={onClose} disabled={busy}>Cancelar</button>
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
