import { Link } from 'react-router-dom';

export function GatewaysHubPage() {
  return (
    <>
      <section className="page-header">
        <h1>Gateways</h1>
        <p>Escolha um gateway para acessar credenciais e regras de integração específicas.</p>
      </section>

      <section className="card">
        <h2>Seleção de gateway</h2>
        <p className="muted">Cada gateway possui contexto e dados próprios. O menu acima mantém a navegação escalável.</p>

        <div className="gateway-grid">
          <Link className="gateway-item active" to="/dashboard/gateways/frendz">
            <span className="gateway-name">Frendz</span>
            <span className="gateway-caption">Credenciais e gestão do integrador ativo</span>
          </Link>
          <Link className="gateway-item" to="/dashboard/gateways/sigilopay">
            <span className="gateway-name">SigiloPay</span>
            <span className="gateway-caption">Credenciais (chave pública e secreta) e integração PIX</span>
          </Link>
          <Link className="gateway-item" to="/dashboard/gateways/wintech">
            <span className="gateway-name">Wintech</span>
            <span className="gateway-caption">API Wintech Pagamentos — chaves públicas/secretas e PIX</span>
          </Link>
        </div>
      </section>
    </>
  );
}
