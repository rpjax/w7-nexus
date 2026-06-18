import type {
  GatewaySelectionStrategy,
  TeamAccountDetails,
  TeamGatewayCredentialDetails,
  TeamGatewayGroupDetails,
} from '../../api/types';

export type GatewayScopeDetails = {
  id: string;
  gatewaySelectionStrategy?: GatewaySelectionStrategy;
  strawMen?: TeamAccountDetails[];
  gatewayCredentials?: TeamGatewayCredentialDetails[];
  gatewayCredentialsGroups?: TeamGatewayGroupDetails[];
};

export type AdminGatewayActions = {
  busy: boolean;
  onGatewayStrategyChange: (scopeId: string, strategy: GatewaySelectionStrategy) => void;
  onAssignStrawMan: (scopeId: string) => void;
  onUnassignStrawMan: (scopeId: string, accountId: string) => void;
  onAssignGatewayCredential: (scopeId: string) => void;
  onUnassignGatewayCredential: (scopeId: string, credentialId: string) => void;
  onAssignGatewayGroup: (scopeId: string, groupId: string) => void;
  onUnassignGatewayGroup: (scopeId: string, groupId: string) => void;
};

export type GatewaySectionVariant = 'team' | 'operation';

export const GATEWAY_STRATEGY_OPTIONS: {
  value: GatewaySelectionStrategy;
  label: string;
  hint: string;
  detail: string;
}[] = [
  {
    value: 'PerStrawman',
    label: 'Por laranja',
    hint: 'Filtra credenciais pelo laranja vinculado.',
    detail: 'Ideal quando cada laranja tem credenciais próprias nos gateways.',
  },
  {
    value: 'PerGroup',
    label: 'Por grupo',
    hint: 'Usa grupos pré-configurados de credenciais.',
    detail: 'Vincule um ou mais grupos do repositório de gateway.',
  },
  {
    value: 'Manual',
    label: 'Manual',
    hint: 'Lista fixa de credenciais escolhidas.',
    detail: 'Controle total sobre quais credenciais entram no roteamento.',
  },
];

export const GATEWAY_LABELS: Record<string, string> = {
  frendz: 'Frendz',
  sigilopay: 'SigiloPay',
  wintech: 'Wintech',
  desconhecido: 'Gateway',
};
