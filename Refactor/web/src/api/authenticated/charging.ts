import { apiClient } from '@/api/client';
import type { Charge } from '@/api/administrator/charging';

export async function listMyCharges() {
  return apiClient.get<{ items: Charge[] }>('/api/charging/authenticated', {
    fallbackError: 'Não foi possível listar as suas cobranças.',
  });
}

export async function getMyCharge(chargeId: string) {
  return apiClient.get<Charge>(`/api/charging/authenticated/${chargeId}`, {
    fallbackError: 'Não foi possível carregar a cobrança.',
  });
}
