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
  }, { fallbackError: 'Não foi possível entrar. Verifique sua conexão e tente novamente.' });
}

export async function signUpAsOperator(username: string, password: string) {
  return apiClient.post<SignUpResponse>('/api/authentication/sign-up/operator', {
    Username: username,
    Password: password,
  }, { fallbackError: 'Não foi possível criar a conta de operador. Tente novamente.' });
}

export async function signUpAsAdministrator(username: string, password: string, masterKey: string) {
  return apiClient.post<SignUpResponse>('/api/authentication/sign-up/administrator', {
    Username: username,
    Password: password,
  }, {
    fallbackError: 'Não foi possível criar a conta de administrador. Verifique a chave mestra e tente novamente.',
    headers: { Authorization: masterKey },
  });
}
