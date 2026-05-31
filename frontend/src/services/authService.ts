import { apiClient } from './api';
import { storageService } from '../utils/storage';
import type { LoginRequest, RegisterRequest, AuthResponse, User } from '../types/auth';

export const authService = {
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/auth/login', credentials);
    return data;
  },

  async register(credentials: RegisterRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/auth/register', credentials);
    return data;
  },

  async getMe(): Promise<User> {
    const { data } = await apiClient.get<User>('/auth/me');
    return data;
  },

  setToken(token: string, rememberMe: boolean = false) {
    storageService.setToken(token, rememberMe);
  },

  getToken(): string | null {
    return storageService.getToken();
  },

  removeToken() {
    storageService.removeToken();
  },
};
