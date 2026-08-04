import { useNavigate, useParams } from 'react-router-dom';
import { PaymentDetailPanel } from '../../features/payments/PaymentDetailPanel';
import { PaymentDetailShell } from '../../features/payments/PaymentDetailShell';
import { listPath } from '../../features/payments/paymentPaths';
import { usePaymentAdminActions } from '../../features/payments/usePaymentAdminActions';
import { usePaymentDetail } from '../../features/payments/usePaymentDetail';

export function AdminPaymentDetailPage() {
  const { paymentId = '' } = useParams();
  const navigate = useNavigate();
  const { payment, loading, error, notFound, reload } = usePaymentDetail('global-admin', paymentId);

  const { actionBar, modals } = usePaymentAdminActions({
    payment,
    onMutated: reload,
    onDeleted: () => navigate(listPath('global-admin')),
  });

  return (
    <>
      <PaymentDetailShell
        kicker="Administração"
        kickerVariant="admin"
        description="Transições do domínio, com contexto legível e ações administrativas ao lado."
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Todos os pagamentos', href: listPath('global-admin') },
          { label: 'Detalhe' },
        ]}
        loading={loading}
        error={error}
        notFound={notFound}
      >
        {payment ? (
          <PaymentDetailPanel
            payment={payment}
            scope="global-admin"
            actionsSlot={actionBar}
          />
        ) : null}
      </PaymentDetailShell>
      {modals}
    </>
  );
}
