import { apiClient } from '../client';
import type { StrawManSettings } from '../types';

export async function getAdministratorStrawManSettings(strawManId: string) {
  return apiClient.get<StrawManSettings>(
    `/api/administrator/straw-men/${encodeURIComponent(strawManId)}/settings`,
    { fallbackError: 'Não foi possível carregar as configurações do laranja.' },
  );
}

export async function upsertStrawManSettings(strawManId: string, movementFeePercentage: number) {
  return apiClient.put<StrawManSettings>(
    `/api/administrator/straw-men/${encodeURIComponent(strawManId)}/settings`,
    { MovementFeePercentage: movementFeePercentage },
    { fallbackError: 'Não foi possível salvar as configurações do laranja.' },
  );
}
