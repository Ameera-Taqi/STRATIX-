const TOKEN_KEY = 'stratix.token';
const REMEMBER_KEY = 'stratix.rememberMe';

export function getAuthToken(): string | null {
  return localStorage.getItem(TOKEN_KEY) ?? sessionStorage.getItem(TOKEN_KEY);
}

export function setAuthToken(token: string, remember: boolean): void {
  if (remember) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(REMEMBER_KEY, '1');
    sessionStorage.removeItem(TOKEN_KEY);
    return;
  }

  sessionStorage.setItem(TOKEN_KEY, token);
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REMEMBER_KEY);
}

export function clearAuthToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REMEMBER_KEY);
}

export function isRememberMeEnabled(): boolean {
  return localStorage.getItem(REMEMBER_KEY) === '1';
}
