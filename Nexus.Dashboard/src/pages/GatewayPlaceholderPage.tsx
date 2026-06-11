import { EmptyState } from '../components/EmptyState';

type GatewayPlaceholderPageProps = {
  title: string;
};

export function GatewayPlaceholderPage({ title }: GatewayPlaceholderPageProps) {
  return (
    <>
      <section className="page-header">
        <h1>{title}</h1>
        <p>Espaço reservado para o próximo módulo de integração de pagamento.</p>
      </section>

      <section className="card">
        <EmptyState
          title="Gateway em planejamento"
          message="A estrutura de navegação já está pronta. Quando o integrador for implementado, os casos de uso entram aqui sem redesenhar o dashboard."
        />
      </section>
    </>
  );
}
