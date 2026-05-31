import { apiClient } from './api';
import type { Conversation } from '../types';

export interface CreateConversationRequest {
  title?: string;
  modelName?: string;
}

export const conversationService = {
  createConversation: async (request: CreateConversationRequest = {}): Promise<Conversation> => {
    const response = await apiClient.post<Conversation>('/conversations', request);
    return response.data;
  },

  getConversations: async (): Promise<Conversation[]> => {
    const response = await apiClient.get<Conversation[]>('/conversations');
    return response.data;
  },

  getConversation: async (id: string): Promise<Conversation> => {
    const response = await apiClient.get<Conversation>(`/conversations/${id}`);
    return response.data;
  },

  deleteConversation: async (id: string): Promise<void> => {
    await apiClient.delete(`/conversations/${id}`);
  },
};
