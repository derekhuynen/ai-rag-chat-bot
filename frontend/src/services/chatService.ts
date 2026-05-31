import { apiClient, API_BASE_URL } from './api';
import { storageService } from '../utils/storage';
import type { ChatRequest, ChatResponse, DocumentCitation } from '../types';

/**
 * Error thrown when the streaming endpoint fails. Carries the HTTP status (when
 * available) so callers can distinguish auth failures (401/403) from transient
 * ones.
 */
export class StreamError extends Error {
  status?: number;
  constructor(message: string, status?: number) {
    super(message);
    this.name = 'StreamError';
    this.status = status;
  }
}

export const chatService = {
  sendMessage: async (request: ChatRequest): Promise<ChatResponse> => {
    const response = await apiClient.post<ChatResponse>('/chat', {
      useRAG: true,
      ...request,
    });
    return response.data;
  },

  streamMessage: async (
    request: ChatRequest,
    onChunk: (content: string) => void,
    onComplete: (citations?: DocumentCitation[]) => void,
    onError: (error: Error) => void,
    signal?: AbortSignal
  ): Promise<void> => {
    try {
      const token = storageService.getToken();
      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      };

      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const response = await fetch(`${API_BASE_URL}/chat/stream`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          useRAG: true,
          ...request,
        }),
        signal,
      });

      if (!response.ok) {
        // Try to surface the server's error body, but fall back to the status.
        let detail = '';
        try {
          detail = await response.text();
        } catch {
          // ignore body read failures
        }
        const message =
          detail?.trim() ||
          `Stream request failed (HTTP ${response.status} ${response.statusText})`;
        throw new StreamError(message, response.status);
      }

      const reader = response.body?.getReader();
      const decoder = new TextDecoder();
      let latestCitations: DocumentCitation[] | undefined;

      if (!reader) {
        throw new StreamError('No response body');
      }

      // SSE events are separated by a blank line ("\n\n"). Accumulate decoded
      // text in a buffer and only process complete events, keeping any trailing
      // partial event for the next read so data isn't dropped across chunk
      // boundaries.
      let buffer = '';

      const processEvent = (rawEvent: string): boolean => {
        // An event may span multiple "data:" lines; the SSE spec joins them
        // with newlines, but this server emits one data line per event.
        for (const line of rawEvent.split('\n')) {
          if (!line.startsWith('data: ')) {
            continue;
          }
          const data = line.slice(6);

          if (data === '[DONE]') {
            onComplete(latestCitations);
            return true; // signal stream complete
          }

          try {
            const parsed = JSON.parse(data);
            if (parsed.content) {
              onChunk(parsed.content);
            }
            if (parsed.citations) {
              latestCitations = parsed.citations as DocumentCitation[];
            }
            if (parsed.error) {
              throw new StreamError(parsed.error);
            }
          } catch (err) {
            if (err instanceof StreamError) {
              throw err;
            }
            // Ignore JSON parse errors for partial/non-JSON data lines.
          }
        }
        return false;
      };

      while (true) {
        const { done, value } = await reader.read();

        if (done) {
          break;
        }

        buffer += decoder.decode(value, { stream: true });

        // Split into complete events; the last element is a (possibly empty)
        // partial event that we keep buffered for the next read.
        const events = buffer.split('\n\n');
        buffer = events.pop() ?? '';

        for (const event of events) {
          if (processEvent(event)) {
            return;
          }
        }
      }

      // Flush any remaining buffered event after the stream closes.
      buffer += decoder.decode();
      if (buffer.trim()) {
        if (processEvent(buffer)) {
          return;
        }
      }

      // Stream ended without explicit [DONE]; still signal completion.
      onComplete(latestCitations);
    } catch (error) {
      // Aborts are intentional (unmount / new stream);swallow them so we don't
      // surface a spurious error to the UI.
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }
      if ((error as Error)?.name === 'AbortError') {
        return;
      }
      onError(error as Error);
    }
  },
};
