export const SCRIPTS_ADMIN_LIST_PATH = '/dashboard/admin/scripts';

export function isScriptStudioPath(pathname: string): boolean {
  return /^\/dashboard\/admin\/scripts\/[^/]+$/i.test(pathname.replace(/\/$/, ''));
}

export function scriptStudioPath(scriptId: string, tab: 'overview' | 'releases' | 'channels' = 'overview') {
  const params = tab === 'overview' ? '' : `?tab=${tab}`;
  return `${SCRIPTS_ADMIN_LIST_PATH}/${scriptId}${params}`;
}
