export type UserNoticeKind = 'error' | 'success' | 'warning' | 'info';

export type UserNotice = {
  kind: UserNoticeKind;
  message: string;
};

/** Single outbound port for anything the user should see as a notice. */
export type UserNoticePort = {
  report(notice: UserNotice): void;
};

export type ApiFailure = { ok: false; error: string; status?: number };
export type ApiSuccess<T> = { ok: true; data: T | null };
export type ApiResultLike<T> = ApiSuccess<T> | ApiFailure;
