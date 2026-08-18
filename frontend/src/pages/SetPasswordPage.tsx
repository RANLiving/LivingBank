import { useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api, setStoredToken } from '../api/client';
import type { LoginResponse } from '../types';

export default function SetPasswordPage() {
  const [params] = useSearchParams();
  const userId = params.get('userId');
  const token = params.get('token');

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [done, setDone] = useState(false);

  const linkInvalid = !userId || !token;

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (newPassword !== confirmPassword) {
      setError('A confirmação não coincide com a nova password.');
      return;
    }
    if (newPassword.length < 8) {
      setError('A password tem de ter pelo menos 8 caracteres.');
      return;
    }

    setBusy(true);
    try {
      const { data } = await api.post<LoginResponse>('/api/auth/set-password', {
        userId,
        token,
        newPassword,
      });
      setStoredToken(data.token);
      setDone(true);
      setTimeout(() => { window.location.href = '/'; }, 1500);
    } catch (err: any) {
      setError(err?.response?.data?.errors?.join(', ') ?? err?.response?.data?.error ?? 'Link inválido ou expirado.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="lb-login-wrap">
      <div className="lb-login-card">
        <img src="/logo.ico" alt="" width={40} height={40} style={{ marginBottom: 8 }} />
        <h1 style={{ fontSize: 24, marginBottom: 4 }}>Definir password</h1>
        <p className="lb-muted" style={{ marginBottom: 24 }}>Bem-vindo à LivingBank</p>

        {linkInvalid && <div className="lb-error-banner">Link inválido — falta informação. Pede um novo convite ao administrador.</div>}
        {error && <div className="lb-error-banner">{error}</div>}
        {done && <div className="lb-card" style={{ borderColor: '#0a8a2e' }}>Password definida! A entrar…</div>}

        {!linkInvalid && !done && (
          <form onSubmit={handleSubmit}>
            <div className="lb-field">
              <label>Nova password</label>
              <input className="lb-input" type="password" required minLength={8} value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
            </div>
            <div className="lb-field">
              <label>Confirmar password</label>
              <input className="lb-input" type="password" required minLength={8} value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
            </div>
            <button className="lb-btn" type="submit" disabled={busy} style={{ width: '100%' }}>
              {busy ? 'A guardar…' : 'Definir password e entrar'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
