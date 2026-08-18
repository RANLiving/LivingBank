import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';

interface Aspsp {
  name: string;
  country: string;
  logo: string | null;
}

const COUNTRIES = ['PT', 'ES', 'FR', 'DE', 'IT', 'GB', 'NL', 'BE', 'LU'];

export default function LinkAccountPage() {
  const [country, setCountry] = useState('PT');
  const [aspsps, setAspsps] = useState<Aspsp[]>([]);
  const [selected, setSelected] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadAspsps() {
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.get<Aspsp[]>('/api/bank-link/aspsps', { params: { country } });
      const sorted = [...data].sort((a, b) => a.name.localeCompare(b.name, 'pt'));
      setAspsps(sorted);
      setSelected(sorted[0]?.name ?? '');
    } catch {
      setError('Falha ao obter a lista de bancos do Enable Banking.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadAspsps();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [country]);

  async function handleAuthorize(e: FormEvent) {
    e.preventDefault();
    if (!selected) return;
    setError(null);
    try {
      const { data } = await api.post<{ url: string }>('/api/bank-link/authorize', {
        aspspName: selected,
        aspspCountry: country,
      });
      sessionStorage.setItem('lb_link_bankname', selected);
      window.location.href = data.url;
    } catch {
      setError('Falha ao iniciar autorização com o Enable Banking.');
    }
  }

  return (
    <div>
      <h1 style={{ fontSize: 24, marginBottom: 8 }}>Ligar conta bancária</h1>
      <p className="lb-muted" style={{ marginBottom: 16 }}>
        Escolhe o banco. Vais ser redirecionado para autenticares e autorizares o acesso via Enable Banking.
      </p>

      {error && <div className="lb-error-banner">{error}</div>}

      <form onSubmit={handleAuthorize} className="lb-card" style={{ maxWidth: 480 }}>
        <div className="lb-field">
          <label>País</label>
          <select className="lb-input" value={country} onChange={(e) => setCountry(e.target.value)}>
            {COUNTRIES.map((c) => <option key={c} value={c}>{c}</option>)}
          </select>
        </div>

        <div className="lb-field">
          <label>Banco</label>
          <select className="lb-input" value={selected} onChange={(e) => setSelected(e.target.value)} disabled={loading}>
            {loading && <option>A carregar…</option>}
            {!loading && aspsps.length === 0 && <option>Nenhum banco encontrado</option>}
            {aspsps.map((a) => <option key={a.name} value={a.name}>{a.name}</option>)}
          </select>
        </div>

        <button className="lb-btn" type="submit" disabled={loading || !selected}>
          Autorizar no banco
        </button>
      </form>
    </div>
  );
}
