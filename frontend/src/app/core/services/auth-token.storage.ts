const ACCESS_TOKEN_KEY = 'stratix.token';
const REFRESH_TOKEN_KEY = 'stratix.refreshToken';
const REMEMBER_KEY = 'stratix.rememberMe';

function read(key: string): string | null {
  return localStorage.getItem(key) ?? sessionStorage.getItem(key);
}

function write(key: string, value: string, remember: boolean): void {
  if (remember) {
    localStorage.setItem(key, value);
    sessionStorage.removeItem(key);
    return;
  }
  sessionStorage.setItem(key, value);
  localStorage.removeItem(key);
}

function remove(key: string): void {
  localStorage.removeItem(key);
  sessionStorage.removeItem(key);
}

export function getAuthToken(): string | null {
  return read(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return read(REFRESH_TOKEN_KEY);
}

export function setAuthTokens(accessToken: string, refreshToken: string | null | undefined, remember: boolean): void {
  write(ACCESS_TOKEN_KEY, accessToken, remember);
  if (refreshToken) {
    write(REFRESH_TOKEN_KEY, refreshToken, remember);
  } else {
    remove(REFRESH_TOKEN_KEY);
  }
  if (remember) {
    localStorage.setItem(REMEMBER_KEY, '1');
  } else {
    localStorage.removeItem(REMEMBER_KEY);
  }
}

/** @deprecated Prefer setAuthTokens — kept for call-site compatibility during migration. */
export function setAuthToken(token: string, remember: boolean): void {
  setAuthTokens(token, getRefreshToken(), remember);
}

export function clearAuthToken(): void {
  remove(ACCESS_TOKEN_KEY);
  remove(REFRESH_TOKEN_KEY);
  localStorage.removeItem(REMEMBER_KEY);
}

export function isRememberMeEnabled(): boolean {
  return localStorage.getItem(REMEMBER_KEY) === '1';
}
