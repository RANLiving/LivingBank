import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { Company } from '../types';

export default function CompaniesPage() {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [form, setForm] = useState({ name: '', taxId: '', address: '' });
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function load() {
    const { data } = await api.get<Company[]>('/api/companies');
    setCompanies(data);
  }

  useEffect(() => {
    load();
  }, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await api.post('/api/companies', form);
      setForm({ name: '', taxId: '', address: '' });
      await load();
    } catch (err: any) {
      setError(err?.response?.data?.error ?? 'Falha ao criar empresa.');
    } finally {
      setBusy(false);
    }
  }

  async function toggleActive(c: Company) {
    await api.patch(`/api/companies/${c.id}/${c.isActive ? 'deactivate' : 'activate'}`);
    await load();
  }

  async function handleDelete(c: Company) {
    if (!confirm(`Eliminar definitivamente "${c.name}"? Esta ação não pode ser desfeita.`)) return;
    setError(null);
    try {
      await api.delete(`/api/companies/${c.id}`);
      await load();
    } catch (err: any) {
      setError(err?.response?.data?.error ?? 'Falha ao eliminar empresa.');
    }
  }

  return (
    <div>
      <h1 style={{ fontSize: 24, marginBottom: 16 }}>Empresas</h1>

      <div className="lb-card">
        <h2 style={{ fontSize: 16, marginBottom: 12 }}>Nova empresa</h2>
        {error && <div className="lb-error-banner">{error}</div>}
        <form onSubmit={handleCreate} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
          <div className="lb-field">
            <label>Nome</label>
            <input className="lb-input" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </div>
          <div className="lb-field">
            <label>NIF</label>
            <input className="lb-input" required value={form.taxId} onChange={(e) => setForm({ ...form, taxId: e.target.value })} />
          </div>
          <div className="lb-field">
            <label>Morada (opcional)</label>
            <input className="lb-input" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
          </div>
          <div style={{ gridColumn: '1 / -1' }}>
            <button className="lb-btn" disabled={busy}>{busy ? 'A criar…' : 'Criar empresa'}</button>
          </div>
        </form>
      </div>

      <table className="lb-table">
        <thead>
          <tr>
            <th>Nome</th>
            <th>NIF</th>
            <th>Morada</th>
            <th>Contas ligadas</th>
            <th>Estado</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {companies.map((c) => (
            <tr key={c.id}>
              <td>{c.name}</td>
              <td>{c.taxId}</td>
              <td className="lb-muted">{c.address || '—'}</td>
              <td>{c.bankAccountCount}</td>
              <td>
                <span className={`lb-badge ${c.isActive ? 'lb-badge-success' : 'lb-badge-error'}`}>
                  {c.isActive ? 'Ativa' : 'Inativa'}
                </span>
              </td>
              <td style={{ display: 'flex', gap: 8 }}>
                <button className="lb-btn-outline" onClick={() => toggleActive(c)}>
                  {c.isActive ? 'Desativar' : 'Ativar'}
                </button>
                {c.bankAccountCount === 0 && (
                  <button className="lb-btn-outline" onClick={() => handleDelete(c)}>Eliminar</button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {companies.length === 0 && <p className="lb-muted">Nenhuma empresa registada.</p>}
    </div>
  );
}
