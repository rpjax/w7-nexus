export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export type AuthLevel = 'none' | 'jwt' | 'master-token';

export type ApiParam = {
  name: string;
  type: string;
  required?: boolean;
  description: string;
};

export type ApiEndpoint = {
  id: string;
  method: HttpMethod;
  path: string;
  title: string;
  description: string;
  summary?: string;
  whenToUse?: string;
  auth: AuthLevel;
  groupId: string;
  requestBody?: string;
  responseBody?: string;
  queryParams?: ApiParam[];
  pathParams?: ApiParam[];
  notes?: string[];
  relatedEndpointIds?: string[];
};

export type ApiFlowStep = {
  title: string;
  summary: string;
  narrative: string;
  why: string;
  outcome: string;
  endpointId?: string;
  pitfalls?: string[];
  tip?: string;
};

export type ApiFlow = {
  id: string;
  title: string;
  description: string;
  accent: 'blue' | 'green' | 'amber' | 'violet' | 'rose';
  audience: string;
  prerequisites: string[];
  estimatedMinutes: number;
  outcome: string;
  steps: ApiFlowStep[];
};

export type ApiGroup = {
  id: string;
  title: string;
  description: string;
  intro: string;
};

export type ApiDocsView =
  | { kind: 'overview' }
  | { kind: 'flow'; id: string }
  | { kind: 'endpoint'; id: string }
  | { kind: 'group'; id: string };
