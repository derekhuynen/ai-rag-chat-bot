import { apiClient } from './api';
import type { ImageAttachment } from '../types/chat';

export const imageUploadService = {
  /**
   * Uploads multiple images to blob storage
   * @param files - Array of File objects to upload
   * @param conversationId - Optional conversation ID to associate images with
   * @returns Promise with array of ImageAttachment metadata
   */
  uploadImages: async (files: File[], conversationId?: string): Promise<ImageAttachment[]> => {
    const formData = new FormData();

    files.forEach((file) => {
      formData.append('files', file);
    });

    const url = conversationId
      ? `/image/upload?conversationId=${conversationId}`
      : '/image/upload';

    const response = await apiClient.post<{ message: string; images: ImageAttachment[] }>(
      url,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );

    return response.data.images;
  },

  /**
   * Validates a file before upload
   */
  validateFile: (file: File): { valid: boolean; error?: string } => {
    const maxSize = 20 * 1024 * 1024; // 20MB
    const allowedTypes = [
      'image/png',
      'image/jpeg',
      'image/jpg',
      'image/gif',
      'image/webp',
      'image/bmp',
    ];

    if (!allowedTypes.includes(file.type)) {
      return {
        valid: false,
        error: `Invalid file type: ${file.type}. Allowed types: PNG, JPEG, GIF, WEBP, BMP`,
      };
    }

    if (file.size > maxSize) {
      const sizeMB = (file.size / (1024 * 1024)).toFixed(2);
      return {
        valid: false,
        error: `File too large: ${sizeMB}MB (max 20MB)`,
      };
    }

    return { valid: true };
  },

  /**
   * Validates multiple files
   */
  validateFiles: (files: File[]): { valid: boolean; error?: string } => {
    if (files.length > 10) {
      return {
        valid: false,
        error: 'Maximum 10 images allowed per message',
      };
    }

    for (const file of files) {
      const validation = imageUploadService.validateFile(file);
      if (!validation.valid) {
        return validation;
      }
    }

    return { valid: true };
  },
};
