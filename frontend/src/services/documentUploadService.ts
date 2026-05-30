import { apiClient } from './api';
import type { DocumentAttachment } from '../types/chat';

const MAX_DOCUMENT_SIZE = 10 * 1024 * 1024; // 10MB
const MAX_DOCUMENTS = 5;
const ALLOWED_TYPES = [
  'application/pdf',
  'text/plain',
  'text/markdown',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
];
const ALLOWED_EXTENSIONS = ['.pdf', '.txt', '.md', '.doc', '.docx'];

export const documentUploadService = {
  uploadDocuments: async (
    files: File[],
    conversationId?: string
  ): Promise<DocumentAttachment[]> => {
    const formData = new FormData();
    files.forEach((file) => {
      formData.append('files', file);
    });

    const url = conversationId
      ? `/document/upload?conversationId=${conversationId}`
      : '/document/upload';

    const response = await apiClient.post<{ message: string; documents: DocumentAttachment[] }>(
      url,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );

    return response.data.documents;
  },

  validateFile: (file: File): { valid: boolean; error?: string } => {
    // Check file size
    if (file.size > MAX_DOCUMENT_SIZE) {
      return {
        valid: false,
        error: `File "${file.name}" exceeds maximum size of 10MB`,
      };
    }

    // Check file type
    const extension = file.name.toLowerCase().slice(file.name.lastIndexOf('.'));
    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      return {
        valid: false,
        error: `File type "${extension}" not supported. Allowed types: PDF, TXT, MD, DOC, DOCX`,
      };
    }

    if (!ALLOWED_TYPES.includes(file.type)) {
      return {
        valid: false,
        error: `MIME type "${file.type}" not supported`,
      };
    }

    return { valid: true };
  },

  validateFiles: (files: File[]): { valid: boolean; error?: string } => {
    if (files.length === 0) {
      return {
        valid: false,
        error: 'No files selected',
      };
    }

    if (files.length > MAX_DOCUMENTS) {
      return {
        valid: false,
        error: `Maximum ${MAX_DOCUMENTS} documents allowed per message`,
      };
    }

    for (const file of files) {
      const validation = documentUploadService.validateFile(file);
      if (!validation.valid) {
        return validation;
      }
    }

    return { valid: true };
  },
};
