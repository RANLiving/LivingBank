import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { isBiometricAvailable, loginWithBiometrics, saveBiometricCredentials } from '../api/biometric';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [biometricAvailable, setBiometricAvailable] = useState(false);

  useEffect(() => {
    isBiometricAvailable().then(setBiometricAvailable);
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await login(userName, password);
      await saveBiometricCredentials(userName, password);
      navigate('/');
    } catch {
      setError('Utilizador ou palavra-passe inválidos.');
    } finally {
      setBusy(false);
    }
  }

  async function handleBiometricLogin() {
    setError(null);
    setBusy(true);
    try {
      const creds = await loginWithBiometrics();
      if (!creds) {
        setError('Autenticação biométrica não disponível ou sem credenciais guardadas.');
        return;
      }
      await login(creds.userName, creds.password);
      navigate('/');
    } catch {
      setError('Falha na autenticação biométrica.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="lb-login-wrap">
      <div className="lb-login-card">
        <img src="/logo.ico" alt="" width={40} height={40} style={{ marginBottom: 8 }} />
        <h1 style={{ fontSize: 28, marginBottom: 4 }}>LivingBank</h1>
        <p className="lb-muted" style={{ marginBottom: 24 }}>Iniciar sessão</p>

        {error && <div className="lb-error-banner">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="lb-field">
            <label>Utilizador</label>
            <input className="lb-input" value={userName} onChange={(e) => setUserName(e.target.value)} required />
          </div>
          <div className="lb-field">
            <label>Palavra-passe</label>
            <input className="lb-input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </div>
          <button className="lb-btn" type="submit" disabled={busy} style={{ width: '100%' }}>
            {busy ? 'A entrar…' : 'Entrar'}
          </button>
        </form>

        {biometricAvailable && (
          <button
            className="lb-btn-outline"
            style={{ width: '100%', marginTop: 12 }}
            onClick={handleBiometricLogin}
            disabled={busy}
          >
            Entrar com impressão digital
          </button>
        )}
      </div>
    </div>
  );
}
