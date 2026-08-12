import { apiClient } from './client';
import type { AuthenticationTokens, MyProfile } from '@/auth/types';

type SignInResponse = {
  tokens: AuthenticationTokens;
};

type SignUpResponse = {
  accountId: string;
  tokens: AuthenticationTokens;
};

type MyProfileResponse = {
  profile: MyProfile;
};

type ChangePasswordResponse = {
  tokens: AuthenticationTokens;
};

type ChangeUsernameResponse = {
  username: string;
};

export type SignUpAccountType = 'usuario' | 'admin';

export async function signIn(username: string, password: string) {
  return apiClient.post<SignInResponse>('/api/authentication/sign-in', {
    username,
    password,
  }, { fallbackError: 'Não foi possível entrar. Verifique sua conexão e tente novamente.' });
}

export async function signUpAsUsuario(username: string, password: string) {
  return apiClient.post<SignUpResponse>('/api/authentication/sign-up/usuario', {
    username,
    password,
  }, { fallbackError: 'Não foi possível criar a conta. Tente novamente.' });
}

export async function signUpAsAdmin(username: string, password: string, masterKey: string) {
  return apiClient.post<SignUpResponse>('/api/authentication/sign-up/admin', {
    username,
    password,
  }, {
    fallbackError: 'Não foi possível criar a conta de administrador. Verifique a chave mestra e tente novamente.',
    headers: { 'X-Administrator-Create-Token': masterKey },
  });
}

export async function getMyProfile() {
  return apiClient.get<MyProfileResponse>('/api/authentication/me', {
    fallbackError: 'Não foi possível carregar o perfil.',
  });
}

export async function changeMyPassword(currentPassword: string, newPassword: string) {
  return apiClient.post<ChangePasswordResponse>('/api/authentication/me/password', {
    currentPassword,
    newPassword,
  }, { fallbackError: 'Não foi possível alterar a senha.' });
}

export async function changeMyUsername(newUsername: string) {
  return apiClient.post<ChangeUsernameResponse>('/api/authentication/me/username', {
    newUsername,
  }, { fallbackError: 'Não foi possível alterar o usuário.' });
}
