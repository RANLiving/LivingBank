import { useEffect, useRef, useState, type FormEvent } from 'react';
import { api } from '../api/client';
import type { User } from '../types';

const ROLES = ['Admin', 'Manager', 'Viewer'];

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [form, setForm] = useState({ userName: '', email: '', fullName: '', role: 'Viewer' });
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState({ email: '', fullName: '', role: 'Viewer' });
  const [editBusy, setEditBusy] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [resendingId, setResendingId] = useState<string | null>(null);
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpenMenuId(null);
      }
    }
    if (openMenuId) document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [openMenuId]);

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
    setInfo(null);
    setBusy(true);
    try {
      const { data } = await api.post('/api/users', form);
      setForm({ userName: '', email: '', fullName: '', role: 'Viewer' });
      if (data?.warning) {
        setInfo(data.warning);
      } else {
        setInfo(`Utilizador criado. Email de convite enviado para ${form.email}.`);
      }
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

  function startEdit(u: User) {
    setEditingId(u.id);
    setEditForm({ email: u.email, fullName: u.fullName, role: u.roles[0] ?? 'Viewer' });
    setEditError(null);
  }

  function cancelEdit() {
    setEditingId(null);
    setEditError(null);
  }

  async function handleSaveEdit(id: string) {
    setEditBusy(true);
    setEditError(null);
    try {
      await api.put(`/api/users/${id}`, editForm);
      setEditingId(null);
      await load();
    } catch (err: any) {
      setEditError(err?.response?.data?.errors?.join(', ') ?? err?.response?.data?.error ?? 'Falha ao guardar alterações.');
    } finally {
      setEditBusy(false);
    }
  }

  async function handleResendInvite(u: User) {
    if (!confirm(`Reenviar convite para ${u.email}? A password atual de ${u.fullName} deixa de funcionar até definir uma nova.`)) return;
    setResendingId(u.id);
    setError(null);
    try {
      await api.post(`/api/users/${u.id}/resend-invite`);
      setInfo(`Convite reenviado para ${u.email}.`);
      await load();
    } catch (err: any) {
      setError(err?.response?.data?.error ?? 'Falha ao reenviar convite.');
    } finally {
      setResendingId(null);
    }
  }

  async function handleDelete(u: User) {
    if (!confirm(`Eliminar definitivamente "${u.fullName}"? Esta ação não pode ser desfeita.`)) return;
    setError(null);
    try {
      await api.delete(`/api/users/${u.id}`);
      await load();
    } catch (err: any) {
      setError(err?.response?.data?.error ?? 'Falha ao eliminar utilizador.');
    }
  }

  return (
    <div>
      <h1 style={{ fontSize: 24, marginBottom: 16 }}>Utilizadores</h1>

      <div className="lb-card">
        <h2 style={{ fontSize: 16, marginBottom: 4 }}>Novo utilizador</h2>
        <p className="lb-muted" style={{ marginBottom: 12 }}>
          É enviado um email com um link para o próprio utilizador definir a password — não a defines aqui.
        </p>
        {error && <div className="lb-error-banner">{error}</div>}
        {info && <div className="lb-card" style={{ borderColor: '#0a8a2e', marginBottom: 12 }}>{info}</div>}
        <form onSubmit={handleCreate} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 12 }}>
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
            <label>Role</label>
            <select className="lb-input" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}>
              {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
            </select>
          </div>
          <div style={{ gridColumn: '1 / -1' }}>
            <button className="lb-btn" disabled={busy}>{busy ? 'A criar…' : 'Criar utilizador e enviar convite'}</button>
          </div>
        </form>
      </div>

      {editError && <div className="lb-error-banner">{editError}</div>}

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
          {users.map((u) => {
            const isEditing = editingId === u.id;
            const isAdmin = u.roles.includes('Admin');
            return (
              <tr key={u.id}>
                <td>{u.userName}</td>
                {isEditing ? (
                  <>
                    <td>
                      <input className="lb-input" value={editForm.fullName} onChange={(e) => setEditForm({ ...editForm, fullName: e.target.value })} />
                    </td>
                    <td>
                      <input className="lb-input" type="email" value={editForm.email} onChange={(e) => setEditForm({ ...editForm, email: e.target.value })} />
                    </td>
                    <td>
                      <select
                        className="lb-input"
                        value={editForm.role}
                        onChange={(e) => setEditForm({ ...editForm, role: e.target.value })}
                        disabled={isAdmin}
                      >
                        {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
                      </select>
                    </td>
                    <td>
                      <span className={`lb-badge ${u.isActive ? 'lb-badge-success' : 'lb-badge-error'}`}>
                        {u.isActive ? 'Ativo' : 'Inativo'}
                      </span>
                    </td>
                    <td>{u.passwordSet ? '—' : <span className="lb-badge lb-badge-error">Convite pendente</span>}</td>
                    <td>{u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString('pt-PT') : '—'}</td>
                    <td style={{ display: 'flex', gap: 8 }}>
                      <button className="lb-btn" disabled={editBusy} onClick={() => handleSaveEdit(u.id)}>
                        {editBusy ? 'A guardar…' : 'Guardar'}
                      </button>
                      <button className="lb-btn-outline" disabled={editBusy} onClick={cancelEdit}>Cancelar</button>
                    </td>
                  </>
                ) : (
                  <>
                    <td>{u.fullName}</td>
                    <td>{u.email}</td>
                    <td>{u.roles.join(', ')}</td>
                    <td>
                      <span className={`lb-badge ${u.isActive ? 'lb-badge-success' : 'lb-badge-error'}`}>
                        {u.isActive ? 'Ativo' : 'Inativo'}
                      </span>
                    </td>
                    <td>{u.passwordSet ? '—' : <span className="lb-badge lb-badge-error">Convite pendente</span>}</td>
                    <td>{u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString('pt-PT') : '—'}</td>
                    <td>
                      <div className="lb-menu-wrap" ref={openMenuId === u.id ? menuRef : undefined}>
                        <button className="lb-btn-outline" onClick={() => setOpenMenuId(openMenuId === u.id ? null : u.id)}>
                          Ações ▾
                        </button>
                        {openMenuId === u.id && (
                          <div className="lb-menu">
                            <button className="lb-menu-item" onClick={() => { startEdit(u); setOpenMenuId(null); }}>
                              Editar
                            </button>
                            {!isAdmin && (
                              <button className="lb-menu-item" onClick={() => { toggleActive(u); setOpenMenuId(null); }}>
                                {u.isActive ? 'Desativar' : 'Ativar'}
                              </button>
                            )}
                            <button
                              className="lb-menu-item"
                              disabled={resendingId === u.id}
                              onClick={() => { handleResendInvite(u); setOpenMenuId(null); }}
                            >
                              {resendingId === u.id ? 'A enviar…' : 'Reenviar convite'}
                            </button>
                            {!isAdmin && !u.lastLoginAt && (
                              <button className="lb-menu-item lb-menu-item-danger" onClick={() => { handleDelete(u); setOpenMenuId(null); }}>
                                Eliminar
                              </button>
                            )}
                          </div>
                        )}
                      </div>
                    </td>
                  </>
                )}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
