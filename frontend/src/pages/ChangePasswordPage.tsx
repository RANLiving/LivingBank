import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, setStoredToken } from '../api/client';
import { useAuth } from '../context/AuthContext';
import type { LoginResponse } from '../types';

export default function ChangePasswordPage() {
  const { user, refreshUser } = useAuth();
  const navigate = useNavigate();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const expired = user?.passwordExpired ?? false;

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (newPassword !== confirmPassword) {
      setError('A confirmação não coincide com a nova password.');
      return;
    }
    if (newPassword.length < 8) {
      setError('A nova password tem de ter pelo menos 8 caracteres.');
      return;
    }

    setBusy(true);
    try {
      const { data } = await api.post<LoginResponse>('/api/auth/change-password', {
        currentPassword,
        newPassword,
      });
      setStoredToken(data.token);
      await refreshUser();
      navigate('/');
    } catch (err: any) {
      setError(err?.response?.data?.errors?.join(', ') ?? 'Falha ao mudar a password. Confirma a password atual.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="lb-login-wrap">
      <div className="lb-login-card">
        <h1 style={{ fontSize: 24, marginBottom: 4 }}>Mudar password</h1>
        {expired ? (
          <p className="lb-muted" style={{ marginBottom: 24 }}>
            A tua password expirou (política de 60 dias). Tens de a mudar para continuar.
          </p>
        ) : (
          <p className="lb-muted" style={{ marginBottom: 24 }}>Define uma nova password.</p>
        )}

        {error && <div className="lb-error-banner">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="lb-field">
            <label>Password atual</label>
            <input className="lb-input" type="password" required value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} />
          </div>
          <div className="lb-field">
            <label>Nova password</label>
            <input className="lb-input" type="password" required minLength={8} value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
          </div>
          <div className="lb-field">
            <label>Confirmar nova password</label>
            <input className="lb-input" type="password" required minLength={8} value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
          </div>
          <button className="lb-btn" type="submit" disabled={busy} style={{ width: '100%' }}>
            {busy ? 'A guardar…' : 'Mudar password'}
          </button>
          {!expired && (
            <button type="button" className="lb-btn-outline" style={{ width: '100%', marginTop: 10 }} onClick={() => navigate('/')}>
              Cancelar
            </button>
          )}
        </form>
      </div>
    </div>
  );
}
