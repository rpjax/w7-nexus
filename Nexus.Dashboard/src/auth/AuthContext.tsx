import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { signIn as apiSignIn, signUpAsAdministrator, signUpAsOperator, type SignUpAccountType } from '../api/auth';
import { isIsoDateExpired, isTokenExpired, userFromAccessToken } from './jwt';
import { clearStoredSession, readStoredSession, writeStoredSession } from './storage';
import { setAccessToken } from './tokenStore';
import type { AuthUser, StoredSession } from './types';

type AuthResult = { ok: true } | { ok: false; error: string };

type SignUpParams = {
  accountType: SignUpAccountType;
  username: string;
  password: string;
  masterKey?: string;
};

type AuthContextValue = {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  signIn: (username: string, password: string) => Promise<AuthResult>;
  signUp: (params: SignUpParams) => Promise<AuthResult>;
  signOut: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function normalizeExpiresAt(value: string): string {
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) return value;
  return new Date(parsed).toISOString();
}

function applySession(session: StoredSession): AuthUser | null {
  if (isIsoDateExpired(session.expiresAt) || isTokenExpired(session.accessToken)) {
    return null;
  }

  const user = userFromAccessToken(session.accessToken);
  if (!user) return null;

  setAccessToken(session.accessToken);
  writeStoredSession(session);
  return user;
}

function persistTokens(tokens: {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}): AuthUser | null {
  const session: StoredSession = {
    accessToken: tokens.accessToken,
    refreshToken: tokens.refreshToken,
    expiresAt: normalizeExpiresAt(tokens.expiresAt),
  };

  return applySession(session);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);

  useEffect(() => {
    const stored = readStoredSession();
    if (!stored) {
      setAccessToken(null);
      setIsInitializing(false);
      return;
    }

    const restored = applySession(stored);
    if (!restored) {
      clearStoredSession();
      setAccessToken(null);
    }

    setUser(restored);
    setIsInitializing(false);
  }, []);

  const signOut = useCallback(() => {
    clearStoredSession();
    setAccessToken(null);
    setUser(null);
  }, []);

  const signIn = useCallback(async (username: string, password: string): Promise<AuthResult> => {
    const result = await apiSignIn(username, password);
    if (!result.ok) return { ok: false, error: result.error };

    const tokens = result.data?.tokens;
    if (!tokens?.accessToken) {
      return { ok: false, error: 'A resposta do servidor não incluiu um token de acesso válido.' };
    }

    const nextUser = persistTokens(tokens);
    if (!nextUser) {
      clearStoredSession();
      setAccessToken(null);
      return { ok: false, error: 'O token de acesso recebido é inválido ou expirado.' };
    }

    setUser(nextUser);
    return { ok: true };
  }, []);

  const signUp = useCallback(async (params: SignUpParams): Promise<AuthResult> => {
    const { accountType, username, password, masterKey } = params;

    const result = accountType === 'administrator'
      ? await signUpAsAdministrator(username, password, masterKey ?? '')
      : await signUpAsOperator(username, password);

    if (!result.ok) return { ok: false, error: result.error };

    const tokens = result.data?.tokens;
    if (!tokens?.accessToken) {
      return { ok: false, error: 'A conta foi criada, mas o servidor não retornou um token de acesso válido.' };
    }

    const nextUser = persistTokens(tokens);
    if (!nextUser) {
      clearStoredSession();
      setAccessToken(null);
      return { ok: false, error: 'O token de acesso recebido é inválido ou expirado.' };
    }

    setUser(nextUser);
    return { ok: true };
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null,
    isInitializing,
    signIn,
    signUp,
    signOut,
  }), [user, isInitializing, signIn, signUp, signOut]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
