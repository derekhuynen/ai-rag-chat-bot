import apiClient from './api';

export interface AdminUser {
  id: string;
  email: string;
  name: string;
  role: string;
  createdAt: string;
}

export interface AdminStats {
  totalUsers: number;
  totalConversations: number;
  totalMessages: number;
  usersWithConversations: number;
}

export const adminService = {
  getAllUsers: async (): Promise<AdminUser[]> => {
    const response = await apiClient.get('/management/users');
    return response.data;
  },

  getStats: async (): Promise<AdminStats> => {
    const response = await apiClient.get('/management/stats');
    return response.data;
  },
};
