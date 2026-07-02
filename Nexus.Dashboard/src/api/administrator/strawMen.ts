import { apiClient } from '../client';
import type { StrawManSettings } from '../types';

export async function getAdministratorStrawManSettings(strawManId: string) {
  return apiClient.get<StrawManSettings>(
    `/api/straw-men/administrator/${encodeURIComponent(strawManId)}/settings`,
    { fallbackError: 'Não foi possível carregar as configurações do laranja.' },
  );
}

export async function upsertStrawManSettings(strawManId: string, movementFeePercentage: number) {
  return apiClient.put<StrawManSettings>(
    `/api/straw-men/administrator/${encodeURIComponent(strawManId)}/settings`,
    { MovementFeePercentage: movementFeePercentage },
    { fallbackError: 'Não foi possível salvar as configurações do laranja.' },
  );
}
