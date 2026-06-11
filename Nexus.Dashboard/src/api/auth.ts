import { apiClient } from './client';
import type { AuthenticationTokens } from '../auth/types';

type SignInResponse = {
  tokens: AuthenticationTokens;
};

type SignUpResponse = {
  accountId: string;
  tokens: AuthenticationTokens;
};

export type SignUpAccountType = 'operator' | 'administrator';

export async function signIn(username: string, password: string) {
  return apiClient.post<SignInResponse>('/api/authentication/sign-in', {
    Username: username,
    Password: password,
  }, { fallbackError: 'Falha ao entrar.' });
}

export async function signUpAsOperator(username: string, password: string) {
  return apiClient.post<SignUpResponse>('/api/authentication/sign-up/operator', {
    Username: username,
    Password: password,
  }, { fallbackError: 'Falha ao criar conta de operador.' });
}

export async function signUpAsAdministrator(username: string, password: string, masterKey: string) {
  return apiClient.post<SignUpResponse>('/api/authentication/sign-up/administrator', {
    Username: username,
    Password: password,
  }, {
    fallbackError: 'Falha ao criar conta de administrador.',
    headers: { Authorization: masterKey },
  });
}
