import { apiClient } from './api';

export type DocumentStatus = 'pending' | 'processing' | 'processed' | 'failed';

export interface DocumentListItem {
  id: string;
  fileName: string;
  originalFileName: string;
  uploadedBy: string;
  uploadedAt: string;
  fileSize: number;
  status: DocumentStatus;
  totalChunks: number;
  processingError?: string | null;
}

export interface ListDocumentsResponse {
  documents: DocumentListItem[];
  total: number;
}

export interface DocumentDetail extends DocumentListItem {
  mimeType?: string;
  blobUrl?: string;
}

export interface UploadMetadata {
  category?: string;
  tags?: string[];
  sourceUrl?: string;
}

export interface DownloadInfo {
  downloadUrl: string;
  fileName: string;
  expiresIn: number;
}

export const documentManagementService = {
  listDocuments: async (status?: DocumentStatus): Promise<ListDocumentsResponse> => {
    const params: Record<string, string> = {};
    if (
      status &&
      status !== 'pending' &&
      status !== 'processing' &&
      status !== 'processed' &&
      status !== 'failed'
    ) {
      // invalid status, ignore filter
    } else if (status) {
      params.status = status;
    }

    const response = await apiClient.get<ListDocumentsResponse>('/management/documents', {
      params,
    });
    return response.data;
  },

  uploadDocument: async (
    file: File,
    metadata?: UploadMetadata
  ): Promise<{ documentId: string; fileName: string; status: string }> => {
    const formData = new FormData();
    formData.append('file', file);

    if (metadata) {
      formData.append('metadata', JSON.stringify(metadata));
    }

    const response = await apiClient.post<{ documentId: string; fileName: string; status: string }>(
      '/management/documents/upload',
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );

    return response.data;
  },

  getDocument: async (id: string): Promise<DocumentDetail> => {
    const response = await apiClient.get<DocumentDetail>(`/management/documents/${id}`);
    return response.data;
  },

  deleteDocument: async (id: string): Promise<void> => {
    await apiClient.delete(`/management/documents/${id}`);
  },

  getDownloadInfo: async (id: string): Promise<DownloadInfo> => {
    const response = await apiClient.get<DownloadInfo>(`/management/documents/${id}/download`);
    return response.data;
  },
};
