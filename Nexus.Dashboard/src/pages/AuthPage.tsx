import { useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import type { SignUpAccountType } from '../api/auth';
import { useAuth } from '../auth/AuthContext';
import { useNotifications } from '../notifications/NotificationContext';

type AuthMode = 'sign-in' | 'sign-up';

export function AuthPage() {
  const { signIn, signUp } = useAuth();
  const { notifyError } = useNotifications();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [mode, setMode] = useState<AuthMode>('sign-in');
  const [accountType, setAccountType] = useState<SignUpAccountType>('operator');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [masterKey, setMasterKey] = useState('');
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedUsername = username.trim();
    const trimmedPassword = password.trim();

    if (!trimmedUsername || !trimmedPassword) {
      notifyError('Usuário e senha são obrigatórios.');
      return;
    }

    if (mode === 'sign-up' && trimmedPassword !== confirmPassword.trim()) {
      notifyError('As senhas não coincidem.');
      return;
    }

    if (mode === 'sign-up' && accountType === 'administrator' && !masterKey.trim()) {
      notifyError('A chave mestra é obrigatória para criar um administrador.');
      return;
    }

    setBusy(true);
    try {
      const result = mode === 'sign-in'
        ? await signIn(trimmedUsername, trimmedPassword)
        : await signUp({
          accountType,
          username: trimmedUsername,
          password: trimmedPassword,
          masterKey: accountType === 'administrator' ? masterKey.trim() : undefined,
        });

      if (!result.ok) {
        notifyError(result.error);
        return;
      }

      const redirect = searchParams.get('redirect');
      navigate(redirect?.startsWith('/') ? redirect : '/dashboard', { replace: true });
    } finally {
      setBusy(false);
    }
  }

  function switchMode(nextMode: AuthMode) {
    if (nextMode === mode) return;
    setMode(nextMode);
    setConfirmPassword('');
    setMasterKey('');
    setAccountType('operator');
  }

  function switchAccountType(nextType: SignUpAccountType) {
    if (nextType === accountType) return;
    setAccountType(nextType);
    if (nextType === 'operator') setMasterKey('');
  }

  return (
    <div className="auth-screen">
      <div className="auth-shell">
        <section className="auth-brand">
          <p className="auth-kicker">Websete Nexus</p>
          <h1>Painel operacional</h1>
          <p className="muted auth-lead">
            Entre com sua conta ou cadastre um operador. Administradores exigem a chave mestra configurada no servidor.
          </p>
        </section>

        <section className="card auth-card">
          <div className="auth-tabs" role="tablist" aria-label="Modo de autenticação">
            <button
              type="button"
              role="tab"
              aria-selected={mode === 'sign-in'}
              className={`auth-tab ${mode === 'sign-in' ? 'is-active' : ''}`}
              onClick={() => switchMode('sign-in')}
            >
              Entrar
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={mode === 'sign-up'}
              className={`auth-tab ${mode === 'sign-up' ? 'is-active' : ''}`}
              onClick={() => switchMode('sign-up')}
            >
              Criar conta
            </button>
          </div>

          <div className="auth-card-body">
            <h2>{mode === 'sign-in' ? 'Sign in' : 'Sign up'}</h2>
            <p className="muted auth-card-lead">
              {mode === 'sign-in'
                ? 'Use suas credenciais para restaurar a sessão neste navegador.'
                : accountType === 'operator'
                  ? 'Cadastro de operador. A sessão permanece salva após recarregar a página.'
                  : 'Cadastro de administrador. Informe a chave mestra para autorizar o seed inicial.'}
            </p>

            {mode === 'sign-up' ? (
              <div className="auth-account-type" role="group" aria-label="Tipo de conta">
                <button
                  type="button"
                  className={`auth-account-type-btn ${accountType === 'operator' ? 'is-active' : ''}`}
                  aria-pressed={accountType === 'operator'}
                  onClick={() => switchAccountType('operator')}
                  disabled={busy}
                >
                  Operador
                </button>
                <button
                  type="button"
                  className={`auth-account-type-btn ${accountType === 'administrator' ? 'is-active' : ''}`}
                  aria-pressed={accountType === 'administrator'}
                  onClick={() => switchAccountType('administrator')}
                  disabled={busy}
                >
                  Administrador
                </button>
              </div>
            ) : null}

            <form className="form-grid auth-form" onSubmit={handleSubmit}>
              {mode === 'sign-up' && accountType === 'administrator' ? (
                <div className="field">
                  <label htmlFor="auth-master-key">Chave mestra</label>
                  <input
                    id="auth-master-key"
                    name="masterKey"
                    type="password"
                    autoComplete="off"
                    value={masterKey}
                    onChange={(event) => setMasterKey(event.target.value)}
                    disabled={busy}
                    required
                  />
                </div>
              ) : null}

              <div className="field">
                <label htmlFor="auth-username">Usuário</label>
                <input
                  id="auth-username"
                  name="username"
                  autoComplete="username"
                  value={username}
                  onChange={(event) => setUsername(event.target.value)}
                  disabled={busy}
                  required
                />
              </div>

              <div className="field">
                <label htmlFor="auth-password">Senha</label>
                <input
                  id="auth-password"
                  name="password"
                  type="password"
                  autoComplete={mode === 'sign-in' ? 'current-password' : 'new-password'}
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  disabled={busy}
                  required
                />
              </div>

              {mode === 'sign-up' ? (
                <div className="field">
                  <label htmlFor="auth-confirm-password">Confirmar senha</label>
                  <input
                    id="auth-confirm-password"
                    name="confirmPassword"
                    type="password"
                    autoComplete="new-password"
                    value={confirmPassword}
                    onChange={(event) => setConfirmPassword(event.target.value)}
                    disabled={busy}
                    required
                  />
                </div>
              ) : null}

              <button type="submit" className="btn btn-primary auth-submit" disabled={busy}>
                {busy ? 'Aguarde…' : mode === 'sign-in' ? 'Entrar' : 'Criar conta'}
              </button>
            </form>
          </div>
        </section>
      </div>
    </div>
  );
}
