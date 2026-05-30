// Storage utility for consistent token management
const TOKEN_KEY = 'auth_token';

export const storageService = {
  /**
   * Get token from storage (checks both localStorage and sessionStorage)
   */
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY) || sessionStorage.getItem(TOKEN_KEY);
  },

  /**
   * Set token in either localStorage (persistent) or sessionStorage (session-only)
   */
  setToken(token: string, rememberMe: boolean = false): void {
    if (rememberMe) {
      localStorage.setItem(TOKEN_KEY, token);
      sessionStorage.removeItem(TOKEN_KEY);
    } else {
      sessionStorage.setItem(TOKEN_KEY, token);
      localStorage.removeItem(TOKEN_KEY);
    }
  },

  /**
   * Remove token from both storages
   */
  removeToken(): void {
    localStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(TOKEN_KEY);
  },

  /**
   * Clear all storage
   */
  clearAll(): void {
    localStorage.clear();
    sessionStorage.clear();
  },
};
