import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { User } from '../types';

const ROLES = ['Admin', 'Manager', 'Viewer'];

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [form, setForm] = useState({ userName: '', email: '', fullName: '', password: '', role: 'Viewer' });
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function load() {
    const { data } = await api.get<User[]>('/api/users');
    setUsers(data);
  }

  useEffect(() => {
    load();
  }, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await api.post('/api/users', form);
      setForm({ userName: '', email: '', fullName: '', password: '', role: 'Viewer' });
      await load();
    } catch (err: any) {
      setError(err?.response?.data?.errors?.join(', ') ?? err?.response?.data?.error ?? 'Falha ao criar utilizador.');
    } finally {
      setBusy(false);
    }
  }

  async function toggleActive(u: User) {
    await api.patch(`/api/users/${u.id}/${u.isActive ? 'deactivate' : 'activate'}`);
    await load();
  }

  return (
    <div>
      <h1 style={{ fontSize: 24, marginBottom: 16 }}>Utilizadores</h1>

      <div className="lb-card">
        <h2 style={{ fontSize: 16, marginBottom: 12 }}>Novo utilizador</h2>
        {error && <div className="lb-error-banner">{error}</div>}
        <form onSubmit={handleCreate} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div className="lb-field">
            <label>Utilizador</label>
            <input className="lb-input" required value={form.userName} onChange={(e) => setForm({ ...form, userName: e.target.value })} />
          </div>
          <div className="lb-field">
            <label>Email</label>
            <input className="lb-input" type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          </div>
          <div className="lb-field">
            <label>Nome completo</label>
            <input className="lb-input" required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
          </div>
          <div className="lb-field">
            <label>Palavra-passe</label>
            <input className="lb-input" type="password" minLength={8} required value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
          </div>
          <div className="lb-field">
            <label>Role</label>
            <select className="lb-input" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}>
              {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
            </select>
          </div>
          <div style={{ alignSelf: 'end' }}>
            <button className="lb-btn" disabled={busy}>{busy ? 'A criar…' : 'Criar utilizador'}</button>
          </div>
        </form>
      </div>

      <table className="lb-table">
        <thead>
          <tr>
            <th>Utilizador</th>
            <th>Nome</th>
            <th>Email</th>
            <th>Roles</th>
            <th>Estado</th>
            <th>Password</th>
            <th>Último login</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id}>
              <td>{u.userName}</td>
              <td>{u.fullName}</td>
              <td>{u.email}</td>
              <td>{u.roles.join(', ')}</td>
              <td>
                <span className={`lb-badge ${u.isActive ? 'lb-badge-success' : 'lb-badge-error'}`}>
                  {u.isActive ? 'Ativo' : 'Inativo'}
                </span>
              </td>
              <td>
                {u.passwordExpired && <span className="lb-badge lb-badge-error">Expirada</span>}
              </td>
              <td>{u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString('pt-PT') : '—'}</td>
              <td>
                {u.roles.includes('Admin') ? (
                  <span className="lb-muted">—</span>
                ) : (
                  <button className="lb-btn-outline" onClick={() => toggleActive(u)}>
                    {u.isActive ? 'Desativar' : 'Ativar'}
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
