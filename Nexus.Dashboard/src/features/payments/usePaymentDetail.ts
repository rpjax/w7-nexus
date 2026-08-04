import { useQuery } from '@tanstack/react-query';
import type { PaymentRow } from '@/api/types';
import { fetchPaymentById, normalizePaymentRow } from './fetchPaymentById';
import type { PaymentScope } from './paymentPaths';

export function usePaymentDetail(scope: PaymentScope, paymentId: string | undefined) {
  const trimmedId = paymentId?.trim() ?? '';

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['payment-detail', scope, trimmedId],
    enabled: Boolean(trimmedId),
    queryFn: async (): Promise<PaymentRow> => {
      const result = await fetchPaymentById(scope, trimmedId);
      if (!result.ok) throw new Error(result.error);
      return normalizePaymentRow(result.data!);
    },
  });

  return {
    payment: data ?? null,
    loading: isLoading,
    error: error instanceof Error ? error.message : null,
    notFound: Boolean(trimmedId) && !isLoading && !data && !error,
    reload: () => void refetch(),
  };
}
