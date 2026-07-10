import type { ApiEndpoint } from '../types';

export const API_ENDPOINTS: ApiEndpoint[] = [
  // Authentication
  {
    id: 'auth-sign-in',
    groupId: 'authentication',
    method: 'POST',
    path: '/api/authentication/sign-in',
    title: 'Login',
    description: 'Autentica um usuário e retorna tokens de acesso e refresh.',
    auth: 'none',
    requestBody: `{
  "username": "operador",
  "password": "••••••••"
}`,
    responseBody: `{
  "authenticationTokens": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
    "expiresAt": "2026-07-10T04:00:00.000Z",
    "tokenType": "Bearer"
  }
}`,
    notes: ['Use o accessToken no header Authorization: Bearer {token} em todas as rotas protegidas.'],
  },
  {
    id: 'auth-sign-up-admin',
    groupId: 'authentication',
    method: 'POST',
    path: '/api/authentication/sign-up/administrator',
    title: 'Cadastro de administrador',
    description: 'Cria a primeira conta administradora. Requer token mestre configurado no servidor.',
    auth: 'master-token',
    requestBody: `{
  "username": "admin",
  "password": "••••••••"
}`,
    responseBody: `{
  "accountId": "507f1f77bcf86cd799439011",
  "authenticationTokens": { ... }
}`,
    notes: [
      'Header: Authorization: {Authentication:AdministratorToken}',
      'Não utiliza o esquema Bearer JWT para este endpoint.',
    ],
  },
  {
    id: 'auth-sign-up-operator',
    groupId: 'authentication',
    method: 'POST',
    path: '/api/authentication/sign-up/operator',
    title: 'Cadastro de operador',
    description: 'Registra uma nova conta com papel de operador.',
    auth: 'none',
    requestBody: `{
  "username": "operador",
  "password": "••••••••"
}`,
    responseBody: `{
  "accountId": "507f1f77bcf86cd799439011",
  "authenticationTokens": { ... }
}`,
  },
  {
    id: 'auth-sign-up-strawman',
    groupId: 'authentication',
    method: 'POST',
    path: '/api/authentication/sign-up/strawman',
    title: 'Cadastro de laranja',
    description: 'Registra uma conta com papel de laranja (straw man).',
    auth: 'none',
    requestBody: `{
  "username": "laranja01",
  "password": "••••••••"
}`,
    responseBody: `{
  "accountId": "507f1f77bcf86cd799439011",
  "authenticationTokens": { ... }
}`,
  },

  // Accounts
  {
    id: 'accounts-create',
    groupId: 'accounts',
    method: 'POST',
    path: '/api/accounts/administrator',
    title: 'Criar conta',
    description: 'Cria uma nova conta no sistema (administrador).',
    auth: 'jwt',
    requestBody: `{
  "username": "nova-conta",
  "password": "••••••••"
}`,
    responseBody: `{
  "accountId": "507f1f77bcf86cd799439011"
}`,
  },
  {
    id: 'accounts-search',
    groupId: 'accounts',
    method: 'POST',
    path: '/api/accounts/administrator/search',
    title: 'Buscar contas',
    description: 'Pesquisa contas com paginação e filtro por palavra-chave.',
    auth: 'jwt',
    requestBody: `{
  "keyword": "operador",
  "limit": 20,
  "offset": 0
}`,
    responseBody: `{
  "items": [
    {
      "id": "507f1f77bcf86cd799439011",
      "username": "operador",
      "roles": ["Operator"]
    }
  ],
  "total": 1
}`,
  },
  {
    id: 'accounts-grant-role',
    groupId: 'accounts',
    method: 'POST',
    path: '/api/accounts/administrator/roles',
    title: 'Conceder papel',
    description: 'Atribui um papel (role) a uma conta existente.',
    auth: 'jwt',
    requestBody: `{
  "accountId": "507f1f77bcf86cd799439011",
  "role": "Operator"
}`,
    responseBody: `{ "success": true }`,
  },

  // Operations
  {
    id: 'ops-create',
    groupId: 'operations',
    method: 'POST',
    path: '/api/operations/administrator',
    title: 'Criar operação',
    description: 'Cria uma nova operação no ecossistema Nexus.',
    auth: 'jwt',
    requestBody: `{
  "name": "Operação Alpha",
  "description": "Operação principal de vendas"
}`,
    responseBody: `{
  "operationId": "507f1f77bcf86cd799439011"
}`,
  },
  {
    id: 'ops-search',
    groupId: 'operations',
    method: 'POST',
    path: '/api/operations/administrator/search',
    title: 'Buscar operações',
    description: 'Lista operações com filtros e paginação.',
    auth: 'jwt',
    requestBody: `{
  "keyword": "alpha",
  "limit": 20,
  "offset": 0
}`,
    responseBody: `{
  "items": [
    {
      "id": "507f1f77bcf86cd799439011",
      "name": "Operação Alpha",
      "teamCount": 3
    }
  ],
  "total": 1
}`,
  },
  {
    id: 'ops-create-team',
    groupId: 'operations',
    method: 'POST',
    path: '/api/operations/administrator/teams',
    title: 'Criar equipe',
    description: 'Cria uma equipe dentro de uma operação.',
    auth: 'jwt',
    requestBody: `{
  "operationId": "507f1f77bcf86cd799439011",
  "name": "Equipe Norte"
}`,
    responseBody: `{
  "teamId": "507f1f77bcf86cd799439012"
}`,
  },
  {
    id: 'ops-assign-operator',
    groupId: 'operations',
    method: 'POST',
    path: '/api/operations/administrator/teams/operators',
    title: 'Atribuir operador à equipe',
    description: 'Vincula um operador a uma equipe específica.',
    auth: 'jwt',
    requestBody: `{
  "operationId": "507f1f77bcf86cd799439011",
  "teamId": "507f1f77bcf86cd799439012",
  "operatorId": "507f1f77bcf86cd799439013"
}`,
    responseBody: `{ "success": true }`,
  },
  {
    id: 'ops-operator-search',
    groupId: 'operations',
    method: 'POST',
    path: '/api/operations/operator/search',
    title: 'Minhas operações (operador)',
    description: 'Lista operações visíveis ao operador autenticado.',
    auth: 'jwt',
    requestBody: `{
  "limit": 20,
  "offset": 0
}`,
    responseBody: `{
  "items": [ ... ],
  "total": 5
}`,
  },

  // Payments
  {
    id: 'payments-search-admin',
    groupId: 'payments',
    method: 'POST',
    path: '/api/payments/administrator/search',
    title: 'Buscar pagamentos (admin)',
    description: 'Pesquisa todos os pagamentos do sistema com filtros avançados.',
    auth: 'jwt',
    requestBody: `{
  "keyword": "",
  "status": "Pending",
  "limit": 20,
  "offset": 0
}`,
    responseBody: `{
  "items": [
    {
      "id": "507f1f77bcf86cd799439011",
      "amount": 150.00,
      "status": "Pending",
      "createdAt": "2026-07-10T01:00:00.000Z"
    }
  ],
  "total": 42
}`,
  },
  {
    id: 'payments-detail',
    groupId: 'payments',
    method: 'GET',
    path: '/api/payments/administrator/{paymentId}',
    title: 'Detalhe do pagamento',
    description: 'Retorna informações completas de um pagamento.',
    auth: 'jwt',
    pathParams: [{ name: 'paymentId', type: 'string', description: 'Identificador do pagamento' }],
    responseBody: `{
  "id": "507f1f77bcf86cd799439011",
  "amount": 150.00,
  "status": "Paid",
  "pixCode": "00020126...",
  "operatorId": "507f1f77bcf86cd799439012",
  "createdAt": "2026-07-10T01:00:00.000Z"
}`,
  },
  {
    id: 'payments-pay',
    groupId: 'payments',
    method: 'POST',
    path: '/api/payments/administrator/{paymentId}/pay',
    title: 'Marcar como pago',
    description: 'Confirma manualmente o pagamento de uma cobrança pendente.',
    auth: 'jwt',
    pathParams: [{ name: 'paymentId', type: 'string', description: 'Identificador do pagamento' }],
    responseBody: `{ "status": "Paid", ... }`,
  },
  {
    id: 'payments-refund',
    groupId: 'payments',
    method: 'POST',
    path: '/api/payments/administrator/{paymentId}/refund',
    title: 'Estornar pagamento',
    description: 'Inicia estorno de um pagamento confirmado.',
    auth: 'jwt',
    pathParams: [{ name: 'paymentId', type: 'string', description: 'Identificador do pagamento' }],
    responseBody: `{ "status": "Refunded", ... }`,
  },

  // Charges
  {
    id: 'charges-pix-public',
    groupId: 'charges',
    method: 'POST',
    path: '/api/charges/pix',
    title: 'Gerar cobrança PIX (pública)',
    description: 'Cria uma cobrança PIX sem autenticação. Usado em fluxos de checkout.',
    auth: 'none',
    requestBody: `{
  "operationId": "507f1f77bcf86cd799439011",
  "operatorId": "507f1f77bcf86cd799439012",
  "amount": 150.00
}`,
    responseBody: `{
  "id": "507f1f77bcf86cd799439013",
  "pixCode": "00020126580014br.gov.bcb.pix...",
  "paymentRecipient": "Nome do recebedor",
  "expirationTimeSeconds": 3600
}`,
    notes: ['operatorId é opcional.', 'O pagamento gerado pode ser acompanhado via SignalR.'],
  },
  {
    id: 'charges-pix-admin',
    groupId: 'charges',
    method: 'POST',
    path: '/api/charges/administrator/pix',
    title: 'Gerar cobrança PIX (admin)',
    description: 'Mesma funcionalidade da rota pública, com contexto administrativo.',
    auth: 'jwt',
    requestBody: `{
  "operationId": "507f1f77bcf86cd799439011",
  "operatorId": "507f1f77bcf86cd799439012",
  "amount": 150.00
}`,
    responseBody: `{
  "id": "507f1f77bcf86cd799439013",
  "pixCode": "00020126...",
  "paymentRecipient": "Nome do recebedor",
  "expirationTimeSeconds": 3600
}`,
  },

  // Gateways
  {
    id: 'gateways-search',
    groupId: 'gateways',
    method: 'POST',
    path: '/api/gateways/administrator/{provider}/search',
    title: 'Buscar credenciais',
    description: 'Lista credenciais de um provedor de gateway.',
    auth: 'jwt',
    pathParams: [{ name: 'provider', type: 'enum', description: 'frendz | wintech | sigilopay' }],
    requestBody: `{
  "limit": 20,
  "offset": 0
}`,
    responseBody: `{
  "items": [
    {
      "id": "507f1f77bcf86cd799439011",
      "label": "Conta principal",
      "enabled": true
    }
  ],
  "total": 1
}`,
  },
  {
    id: 'gateways-add-credentials',
    groupId: 'gateways',
    method: 'POST',
    path: '/api/gateways/administrator/{provider}/credentials',
    title: 'Adicionar credenciais',
    description: 'Cadastra novas credenciais para um provedor.',
    auth: 'jwt',
    pathParams: [{ name: 'provider', type: 'enum', description: 'frendz | wintech | sigilopay' }],
    requestBody: `{
  "label": "Conta principal",
  "apiKey": "sk_live_...",
  "enabled": true
}`,
    responseBody: `{
  "id": "507f1f77bcf86cd799439011",
  "label": "Conta principal",
  "enabled": true
}`,
    notes: ['O corpo da requisição varia conforme o provedor.'],
  },

  // Scripts
  {
    id: 'scripts-resolve',
    groupId: 'scripts',
    method: 'GET',
    path: '/scripts',
    title: 'Resolver scripts (público)',
    description: 'Resolve scripts por host, nome e canal. Suporta cache via ETag.',
    auth: 'none',
    queryParams: [
      { name: 'host', type: 'string', description: 'Host de resolução (ex: example.com)' },
      { name: 'name', type: 'string', description: 'Nome do script' },
      { name: 'channel', type: 'string', description: 'Canal (prod, beta, etc.)' },
      { name: 'version', type: 'string', required: false, description: 'Versão específica' },
      { name: 'allowDeprecated', type: 'boolean', required: false, description: 'Permitir releases depreciados' },
    ],
    responseBody: `{
  "items": [
    {
      "name": "checkout-helper",
      "version": "1.2.0",
      "channel": "prod",
      "hash": "a1b2c3...",
      "sourceCode": "// script content...",
      "priority": 0
    }
  ],
  "aggregateHash": "d4e5f6..."
}`,
    notes: ['Retorna 304 Not Modified quando o ETag do cliente coincide.'],
  },
  {
    id: 'scripts-create',
    groupId: 'scripts',
    method: 'POST',
    path: '/api/scripts/administrator',
    title: 'Criar script',
    description: 'Registra um novo script no inventário.',
    auth: 'jwt',
    requestBody: `{
  "name": "checkout-helper",
  "hostPatterns": ["*.example.com"],
  "description": "Auxilia checkout"
}`,
    responseBody: `{
  "scriptId": "507f1f77bcf86cd799439011"
}`,
  },
  {
    id: 'scripts-search',
    groupId: 'scripts',
    method: 'GET',
    path: '/api/scripts/administrator',
    title: 'Buscar scripts',
    description: 'Lista scripts do inventário com filtros.',
    auth: 'jwt',
    queryParams: [
      { name: 'keyword', type: 'string', required: false, description: 'Filtro por nome ou host' },
      { name: 'limit', type: 'number', required: false, description: 'Limite de resultados' },
      { name: 'offset', type: 'number', required: false, description: 'Offset para paginação' },
    ],
    responseBody: `{
  "items": [ ... ],
  "total": 12
}`,
  },
  {
    id: 'scripts-get',
    groupId: 'scripts',
    method: 'GET',
    path: '/api/scripts/administrator/{scriptId}',
    title: 'Detalhe do script',
    description: 'Retorna metadados, canais e versões ativas de um script.',
    auth: 'jwt',
    pathParams: [{ name: 'scriptId', type: 'string', description: 'ID do script' }],
    responseBody: `{
  "id": "507f1f77bcf86cd799439011",
  "name": "checkout-helper",
  "hostPatterns": ["*.example.com"],
  "priority": 0,
  "channels": [
    {
      "routeValue": "prod",
      "displayName": "Produção",
      "version": "1.2.0"
    }
  ]
}`,
  },
  {
    id: 'scripts-update',
    groupId: 'scripts',
    method: 'PATCH',
    path: '/api/scripts/administrator/{scriptId}',
    title: 'Atualizar script',
    description: 'Atualiza prioridade, descrição ou padrões de host do script.',
    auth: 'jwt',
    pathParams: [{ name: 'scriptId', type: 'string', description: 'ID do script' }],
    requestBody: `{
  "priority": 10,
  "description": "Script de checkout atualizado",
  "hostPatterns": ["*.example.com", "shop.example.com"]
}`,
    responseBody: `{ ...ScriptDetailResponse }`,
  },
  {
    id: 'scripts-list-releases',
    groupId: 'scripts',
    method: 'GET',
    path: '/api/scripts/administrator/{scriptId}/releases',
    title: 'Listar releases',
    description: 'Lista todas as releases publicadas de um script.',
    auth: 'jwt',
    pathParams: [{ name: 'scriptId', type: 'string', description: 'ID do script' }],
    responseBody: `{
  "items": [
    {
      "id": "507f1f77bcf86cd799439012",
      "version": "1.2.0",
      "isDeprecated": false
    }
  ]
}`,
  },
  {
    id: 'scripts-get-release',
    groupId: 'scripts',
    method: 'GET',
    path: '/api/scripts/administrator/{scriptId}/releases/{releaseId}',
    title: 'Detalhe da release',
    description: 'Retorna metadados de uma release específica.',
    auth: 'jwt',
    pathParams: [
      { name: 'scriptId', type: 'string', description: 'ID do script' },
      { name: 'releaseId', type: 'string', description: 'ID da release' },
    ],
    responseBody: `{
  "id": "507f1f77bcf86cd799439012",
  "version": "1.2.0",
  "hash": "abc123",
  "sourceCodeSizeBytes": 2048,
  "isDeprecated": false,
  "promotedChannelRouteValues": ["prod"]
}`,
  },
  {
    id: 'scripts-release-source',
    groupId: 'scripts',
    method: 'GET',
    path: '/api/scripts/administrator/{scriptId}/releases/{releaseId}/source-code',
    title: 'Baixar código-fonte',
    description: 'Retorna o código-fonte completo de uma release.',
    auth: 'jwt',
    pathParams: [
      { name: 'scriptId', type: 'string', description: 'ID do script' },
      { name: 'releaseId', type: 'string', description: 'ID da release' },
    ],
    responseBody: `{
  "sourceCode": "// conteúdo do script..."
}`,
  },
  {
    id: 'scripts-publish-release',
    groupId: 'scripts',
    method: 'POST',
    path: '/api/scripts/administrator/{scriptId}/releases',
    title: 'Publicar release',
    description: 'Publica uma nova versão do script. A versão é calculada automaticamente ou informada via Major/Minor/Patch.',
    auth: 'jwt',
    pathParams: [{ name: 'scriptId', type: 'string', description: 'ID do script' }],
    requestBody: `{
  "sourceCode": "// novo código...",
  "major": 1,
  "minor": 2,
  "patch": 0
}`,
    responseBody: `{
  "releaseId": "507f1f77bcf86cd799439012",
  "version": "1.2.0"
}`,
    notes: [
      'Major, Minor e Patch são opcionais — omita para incremento automático.',
      'O campo no JSON é sourceCode (camelCase).',
    ],
  },
  {
    id: 'scripts-promote-channel',
    groupId: 'scripts',
    method: 'POST',
    path: '/api/scripts/administrator/{scriptId}/channels/{channelRouteValue}/promote',
    title: 'Promover release para canal',
    description: 'Associa uma release a um canal (prod, beta, etc.).',
    auth: 'jwt',
    pathParams: [
      { name: 'scriptId', type: 'string', description: 'ID do script' },
      { name: 'channelRouteValue', type: 'string', description: 'Rota do canal (ex: prod)' },
    ],
    requestBody: `{
  "releaseId": "507f1f77bcf86cd799439012"
}`,
    responseBody: `true`,
  },
  {
    id: 'scripts-add-channel',
    groupId: 'scripts',
    method: 'POST',
    path: '/api/scripts/administrator/{scriptId}/channels',
    title: 'Adicionar canal customizado',
    description: 'Cria um canal adicional além dos canais padrão (prod, beta, etc.).',
    auth: 'jwt',
    pathParams: [{ name: 'scriptId', type: 'string', description: 'ID do script' }],
    requestBody: `{
  "customName": "staging"
}`,
    responseBody: `true`,
  },
  {
    id: 'scripts-deprecate-release',
    groupId: 'scripts',
    method: 'POST',
    path: '/api/scripts/administrator/{scriptId}/releases/{releaseId}/deprecate',
    title: 'Depreciar release',
    description: 'Marca uma release como depreciada, impedindo resolução pública.',
    auth: 'jwt',
    pathParams: [
      { name: 'scriptId', type: 'string', description: 'ID do script' },
      { name: 'releaseId', type: 'string', description: 'ID da release' },
    ],
    responseBody: `true`,
  },
  {
    id: 'scripts-restore-release',
    groupId: 'scripts',
    method: 'POST',
    path: '/api/scripts/administrator/{scriptId}/releases/{releaseId}/restore',
    title: 'Restaurar release',
    description: 'Remove a marcação de depreciação de uma release.',
    auth: 'jwt',
    pathParams: [
      { name: 'scriptId', type: 'string', description: 'ID do script' },
      { name: 'releaseId', type: 'string', description: 'ID da release' },
    ],
    responseBody: `true`,
  },
  {
    id: 'scripts-delete-release',
    groupId: 'scripts',
    method: 'DELETE',
    path: '/api/scripts/administrator/{scriptId}/releases/{releaseId}',
    title: 'Excluir release',
    description: 'Remove permanentemente uma release e limpa ponteiros de canais associados.',
    auth: 'jwt',
    pathParams: [
      { name: 'scriptId', type: 'string', description: 'ID do script' },
      { name: 'releaseId', type: 'string', description: 'ID da release' },
    ],
    responseBody: `{
  "releaseId": "507f1f77bcf86cd799439012",
  "clearedChannelRouteValues": ["beta"]
}`,
  },

  // Straw men
  {
    id: 'strawman-settings-self',
    groupId: 'straw-men',
    method: 'GET',
    path: '/api/straw-men/straw-man/settings',
    title: 'Minhas configurações (laranja)',
    description: 'Retorna configurações da conta laranja autenticada.',
    auth: 'jwt',
    responseBody: `{
  "pixKey": "email@exemplo.com",
  "displayName": "Recebedor PIX"
}`,
  },
  {
    id: 'strawman-settings-admin',
    groupId: 'straw-men',
    method: 'PUT',
    path: '/api/straw-men/administrator/{strawManId}/settings',
    title: 'Atualizar configurações (admin)',
    description: 'Atualiza configurações de um laranja específico.',
    auth: 'jwt',
    pathParams: [{ name: 'strawManId', type: 'string', description: 'ID da conta laranja' }],
    requestBody: `{
  "pixKey": "email@exemplo.com",
  "displayName": "Recebedor PIX"
}`,
    responseBody: `{ "pixKey": "...", "displayName": "..." }`,
  },

  // OLX
  {
    id: 'olx-impersonate',
    groupId: 'olx',
    method: 'POST',
    path: '/api/olx/ads/impersonate',
    title: 'Impersonar anúncio',
    description: 'Inicia impersonação de um anúncio OLX para operador.',
    auth: 'jwt',
    requestBody: `{
  "adId": "123456789",
  "adUrl": "https://olx.com.br/anuncio/..."
}`,
    responseBody: `{
  "patchId": "507f1f77bcf86cd799439011"
}`,
  },
  {
    id: 'olx-patch-ad',
    groupId: 'olx',
    method: 'PUT',
    path: '/api/olx/ads/patch',
    title: 'Atualizar patch do anúncio',
    description: 'Aplica alterações ao patch de um anúncio impersonado.',
    auth: 'jwt',
    requestBody: `{
  "patchId": "507f1f77bcf86cd799439011",
  "title": "Novo título",
  "price": 999.90
}`,
    responseBody: `{ "success": true }`,
  },
  {
    id: 'olx-victim-list',
    groupId: 'olx',
    method: 'GET',
    path: '/api/olx/victim/ad-patches',
    title: 'Listar patches (vítima)',
    description: 'Endpoint público que lista anúncios patcheados visíveis à vítima.',
    auth: 'none',
    responseBody: `{
  "items": [
    {
      "adId": "123456789",
      "title": "Produto",
      "price": 999.90
    }
  ]
}`,
  },

  // Realtime
  {
    id: 'signalr-hub',
    groupId: 'realtime',
    method: 'GET',
    path: '/hubs/payment-status',
    title: 'Hub SignalR — Status de pagamento',
    description: 'Conexão WebSocket para receber atualizações em tempo real de pagamentos.',
    auth: 'none',
    notes: [
      'Protocolo: SignalR com método JoinPaymentAsync(paymentId)',
      'Notificação: PaymentStatusChangedNotification',
      'Grupo: payment:{paymentId}',
    ],
    responseBody: `// Cliente JavaScript (SignalR)
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/payment-status')
  .build();

await connection.start();
await connection.invoke('JoinPaymentAsync', paymentId);

connection.on('PaymentStatusChanged', (notification) => {
  console.log(notification.status);
});`,
  },
];

export const endpointById = new Map(API_ENDPOINTS.map((e) => [e.id, e]));

export function endpointsByGroup(groupId: string): ApiEndpoint[] {
  return API_ENDPOINTS.filter((e) => e.groupId === groupId);
}
