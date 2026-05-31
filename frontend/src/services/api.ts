import axios from 'axios';
import { storageService } from '../utils/storage';

// Shared base URL for all API access (axios client, raw fetch for SSE, etc.).
// Exported so other services don't re-declare it (see imageUploadService).
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:7071/api';

// Custom event dispatched when the API rejects the current token (HTTP 401).
// The AuthProvider listens for this to clear in-memory auth state, avoiding a
// hard reload. Components can also react if they need to redirect.
export const AUTH_UNAUTHORIZED_EVENT = 'auth:unauthorized';

// NOTE: The JWT is stored in localStorage/sessionStorage (see storageService),
// which is readable by injected scripts (XSS tradeoff). Moving to an HttpOnly
// cookie would be more secure but requires a coordinated backend change and is
// intentionally out of scope here.

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor for auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = storageService.getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;
    // Treat the auth endpoints themselves as login flows: a 401 there means
    // "bad credentials", not "session expired", so we must not clear/redirect
    // (which would otherwise loop on the login request).
    const requestUrl: string = error.config?.url ?? '';
    const isAuthEndpoint = requestUrl.includes('/auth/login') || requestUrl.includes('/auth/register');

    if (status === 401 && !isAuthEndpoint) {
      // Session expired / token invalid: clear the stored token and notify the
      // app so the AuthProvider can drop the user and redirect to login.
      storageService.removeToken();
      window.dispatchEvent(new CustomEvent(AUTH_UNAUTHORIZED_EVENT));
    }
    return Promise.reject(error);
  }
);

export default apiClient;
