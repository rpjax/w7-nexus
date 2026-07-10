export type ScriptSummary = {
  id: string;
  name: string;
  hostPatterns: string[];
  priority: number;
  description: string | null;
  createdAt: string;
  updatedAt: string;
  channels: ChannelSummary[];
};

export type SearchScriptsResponse = {
  offset: number;
  limit: number;
  total: number;
  items: ScriptSummary[];
};

export type ChannelSummary = {
  routeValue: string;
  displayName: string;
  isCustom: boolean;
  currentReleaseId: string | null;
  version: string | null;
  hash: string | null;
  isDeprecated: boolean | null;
};

export type ScriptDetail = {
  id: string;
  name: string;
  hostPatterns: string[];
  priority: number;
  description: string | null;
  createdAt: string;
  updatedAt: string;
  channels: ChannelSummary[];
};

export type ReleaseSummary = {
  id: string;
  version: string;
  hash: string;
  sourceCodeSizeBytes: number;
  isDeprecated: boolean;
  createdAt: string;
  promotedChannelRouteValues: string[];
};

export type ReleaseListResponse = {
  items: ReleaseSummary[];
};

export type ReleaseDetail = {
  id: string;
  scriptId: string;
  version: string;
  hash: string;
  sourceCodeSizeBytes: number;
  isDeprecated: boolean;
  createdAt: string;
  promotedChannelRouteValues: string[];
};

export type ReleaseSourceCode = {
  sourceCode: string;
};

export type CreateScriptPayload = {
  name: string;
  hostPatterns?: string[];
  priority?: number;
  description?: string | null;
};

export type UpdateScriptPayload = {
  priority?: number;
  description?: string | null;
  hostPatterns?: string[];
};

export type PublishReleasePayload = {
  sourceCode: string;
  major?: number;
  minor?: number;
  patch?: number;
};

export type PublishReleaseResponse = {
  id: string;
  version: string;
  hash: string;
  sourceCodeSizeBytes: number;
};

export type DeleteReleaseResponse = {
  clearedChannelRouteValues: string[];
};

export type ResolutionModeFilter = 'all' | 'host' | 'name-only';
export type ChannelFilter = 'all' | 'prod' | 'staging' | 'development' | 'missing-prod';
