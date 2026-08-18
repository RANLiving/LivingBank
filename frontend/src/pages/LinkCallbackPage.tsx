import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';

interface LinkedAccountOption {
  uid: string;
  iban: string | null;
  name: string | null;
  currency: string;
}

export default function LinkCallbackPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [accounts, setAccounts] = useState<LinkedAccountOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [savingUid, setSavingUid] = useState<string | null>(null);
  const [savedUids, setSavedUids] = useState<string[]>([]);

  const sessionId = params.get('sessionId');
  const paramError = params.get('error');
  const bankName = sessionStorage.getItem('lb_link_bankname') ?? 'Banco';

  useEffect(() => {
    if (paramError) {
      setError(`Enable Banking devolveu um erro: ${paramError}`);
      setLoading(false);
      return;
    }
    if (!sessionId) {
      setError('Sessão inválida — falta sessionId no callback.');
      setLoading(false);
      return;
    }
    api.get(`/api/bank-link/session/${sessionId}/accounts`)
      .then(({ data }) => setAccounts(data.accounts))
      .catch(() => setError('Falha ao obter as contas desta sessão.'))
      .finally(() => setLoading(false));
  }, [sessionId, paramError]);

  async function handleSave(acc: LinkedAccountOption) {
    if (!sessionId) return;
    setSavingUid(acc.uid);
    setError(null);
    try {
      await api.post('/api/bank-link/save', {
        sessionId,
        accountUid: acc.uid,
        iban: acc.iban ?? '',
        bankName,
        displayName: acc.name ?? bankName,
        currency: acc.currency,
      });
      setSavedUids((prev) => [...prev, acc.uid]);
    } catch (err: any) {
      setError(err?.response?.data?.error ?? 'Falha ao guardar a conta.');
    } finally {
      setSavingUid(null);
    }
  }

  return (
    <div>
      <h1 style={{ fontSize: 24, marginBottom: 16 }}>Contas disponíveis — {bankName}</h1>

      {error && <div className="lb-error-banner">{error}</div>}
      {loading && <p>A carregar contas…</p>}

      {!loading && accounts.length === 0 && !error && (
        <p className="lb-muted">Nenhuma conta foi devolvida para esta sessão.</p>
      )}

      <div className="lb-grid">
        {accounts.map((acc) => {
          const saved = savedUids.includes(acc.uid);
          return (
            <div className="lb-card" key={acc.uid}>
              <strong>{acc.name || 'Conta'}</strong>
              <div className="lb-muted">{acc.iban || '—'}</div>
              <div className="lb-muted" style={{ marginBottom: 12 }}>{acc.currency}</div>
              {saved ? (
                <span className="lb-badge lb-badge-success">Guardada</span>
              ) : (
                <button className="lb-btn" disabled={savingUid === acc.uid} onClick={() => handleSave(acc)}>
                  {savingUid === acc.uid ? 'A guardar…' : 'Guardar conta'}
                </button>
              )}
            </div>
          );
        })}
      </div>

      {savedUids.length > 0 && (
        <button className="lb-btn-outline" style={{ marginTop: 16 }} onClick={() => navigate('/')}>
          Ir para as contas
        </button>
      )}
    </div>
  );
}
