import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Layout() {
  const { user, logout, hasRole } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login');
  }

  return (
    <div className="lb-app">
      <header className="lb-topbar">
        <NavLink to="/" className="lb-brand">
          <img src="/logo.ico" alt="" width={24} height={24} style={{ verticalAlign: 'middle', marginRight: 8 }} />
          LivingBank
        </NavLink>
        <nav className="lb-nav">
          <NavLink to="/" end>Contas</NavLink>
          {hasRole('Admin', 'Manager') && <NavLink to="/link">Ligar conta</NavLink>}
          {hasRole('Admin') && <NavLink to="/users">Utilizadores</NavLink>}
          {hasRole('Admin') && <NavLink to="/logs">Logs</NavLink>}
          {hasRole('Admin', 'Manager') && <NavLink to="/schedule">Agendamento</NavLink>}
          <NavLink to="/change-password">Mudar password</NavLink>
          <span className="lb-muted">{user?.fullName}</span>
          <button className="lb-btn-outline" onClick={handleLogout}>Sair</button>
        </nav>
      </header>
      <main className="lb-main">
        <Outlet />
      </main>
    </div>
  );
}
