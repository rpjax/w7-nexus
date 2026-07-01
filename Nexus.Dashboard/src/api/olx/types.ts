export type OlxOperatorAdPatchRow = {
  id: string;
  operationId: string;
  adId: string;
  adUrl: string;
  isImpersonating: boolean;
  originalPrice?: number | null;
  promotionalPrice?: number | null;
  createdAt: string;
  updatedAt: string;
};

export type OlxAdminAdPatchRow = OlxOperatorAdPatchRow & {
  operatorId?: string | null;
};

export type OlxSearchResponse<T> = {
  offset: number;
  limit: number;
  total: number;
  items: T[];
};

export type OlxOperatorSearchRequest = {
  limit: number;
  offset: number;
  keyword?: string | null;
  operationIds?: string[];
};

export type OlxAdminSearchRequest = OlxOperatorSearchRequest & {
  operatorIds?: string[];
};

export type ImpersonateAdPayload = {
  operationId: string;
  operatorId: string;
  adId: string;
  adUrl: string;
};

export type UnimpersonateAdPayload = {
  operationId: string;
  operatorId: string;
  adId: string;
};

export type UpdateAdPatchPayload = {
  operationId: string;
  adId: string;
  originalPrice?: number | null;
  promotionalPrice?: number | null;
};
