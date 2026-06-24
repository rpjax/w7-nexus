import { apiClient } from '../client';
import type { StrawManSettings } from '../types';

export async function getStrawManSettings() {
  return apiClient.get<StrawManSettings>('/api/straw-man/settings', {
    fallbackError: 'Não foi possível carregar as configurações do laranja.',
  });
}
