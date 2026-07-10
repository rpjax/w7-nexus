import type { ApiGroup } from '../types';

export const API_GROUPS: ApiGroup[] = [
  {
    id: 'authentication',
    title: 'Autenticação',
    description: 'Cadastro e login com emissão de tokens JWT.',
    intro: 'Toda integração começa aqui. O Nexus usa JWT Bearer com papéis embutidos no token. Rotas públicas são exceção — a maioria exige autenticação após o sign-in.',
  },
  {
    id: 'accounts',
    title: 'Contas',
    description: 'Gestão de contas, papéis e permissões.',
    intro: 'Administradores criam contas, concedem papéis (Operator, Administrator, etc.) e ajustam permissões granulares. Use estas rotas para provisionar usuários sem recriar credenciais manualmente.',
  },
  {
    id: 'operations',
    title: 'Operações',
    description: 'Operações, equipes, atribuições e estratégias de gateway.',
    intro: 'Uma operação é o container organizacional do Nexus: agrupa equipes, operadores, laranjas e gateways. Antes de gerar cobranças, a estrutura operacional precisa existir.',
  },
  {
    id: 'payments',
    title: 'Pagamentos',
    description: 'Consulta e ações administrativas sobre pagamentos.',
    intro: 'Pagamentos nascem de cobranças PIX e evoluem por webhooks de gateway e ações manuais. Administradores têm visão global; operadores veem apenas os seus.',
  },
  {
    id: 'charges',
    title: 'Cobranças PIX',
    description: 'Geração de cobranças PIX públicas e administrativas.',
    intro: 'A cobrança é o ponto de entrada do fluxo financeiro. A rota pública permite checkout sem login; a administrativa usa o mesmo contrato com contexto autenticado.',
  },
  {
    id: 'gateways',
    title: 'Gateways',
    description: 'Credenciais dos provedores Frendz, Wintech e SigiloPay.',
    intro: 'Gateways são os conectores PIX. Cada operação escolhe credenciais ativas; sem gateway configurado, cobranças falham na etapa de roteamento.',
  },
  {
    id: 'scripts',
    title: 'Scripts',
    description: 'Resolução pública e administração de releases e canais.',
    intro: 'Scripts são injetados em páginas de clientes via resolução por host. O ciclo de vida passa por inventário → release → promoção em canal → resolução em runtime com cache ETag.',
  },
  {
    id: 'straw-men',
    title: 'Laranjas',
    description: 'Configurações de contas laranja.',
    intro: 'Laranjas são contas recebedoras com chave PIX própria. Cada laranja configura seus dados; administradores podem ajustar em nome de terceiros.',
  },
  {
    id: 'olx',
    title: 'OLX',
    description: 'Impersonação de anúncios e patches OLX.',
    intro: 'O módulo OLX permite impersonar anúncios, aplicar patches de preço/título e expor versões modificadas via endpoint público de vítima.',
  },
  {
    id: 'realtime',
    title: 'Tempo real',
    description: 'SignalR para acompanhamento de status de pagamentos.',
    intro: 'Após criar uma cobrança, clientes podem acompanhar mudanças de status em tempo real via hub SignalR, sem polling HTTP.',
  },
];
