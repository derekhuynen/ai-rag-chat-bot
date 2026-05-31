export interface ImageAttachment {
  id: string;
  url: string;
  mimeType: string;
  filename: string;
  size: number;
  blobName: string;
  thumbnailUrl?: string;
}

export interface DocumentAttachment {
  id: string;
  url: string;
  mimeType: string;
  filename: string;
  size: number;
  blobName: string;
  extractedText: string;
  pageCount?: number;
  wordCount: number;
}

export interface DocumentCitation {
  documentId: string;
  documentName: string;
  page: number;
  blobUrl: string;
  relevanceScore: number;
}

export interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  images?: ImageAttachment[];
  documents?: DocumentAttachment[];
  citations?: DocumentCitation[];
}

export interface Conversation {
  id: string;
  title: string;
  messages: Message[];
  createdAt: Date;
  updatedAt: Date;
}

export interface ChatRequest {
  message: string;
  conversationId?: string;
  conversationHistory?: string[];
  images?: ImageAttachment[];
  documents?: DocumentAttachment[];
  useRAG?: boolean;
}

export interface ChatResponse {
  message: string;
  conversationId?: string;
  timestamp: string;
}
