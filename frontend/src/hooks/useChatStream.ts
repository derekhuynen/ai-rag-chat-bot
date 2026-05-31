import { useState, useRef, useEffect, useCallback } from 'react';
import { chatService, StreamError } from '../services/chatService';
import type { Message, ImageAttachment, DocumentAttachment } from '../types';

export function useChatStream() {
  const [isStreaming, setIsStreaming] = useState(false);
  const [streamingMessage, setStreamingMessage] = useState('');

  // Tracks the in-flight request so we can abort it on unmount or when a new
  // stream starts. `mountedRef` guards against setting state after unmount.
  const abortControllerRef = useRef<AbortController | null>(null);
  const mountedRef = useRef(true);

  const stop = useCallback(() => {
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;
    if (mountedRef.current) {
      setIsStreaming(false);
      setStreamingMessage('');
    }
  }, []);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
      // Abort any in-flight stream on unmount.
      abortControllerRef.current?.abort();
      abortControllerRef.current = null;
    };
  }, []);

  const sendStreamingMessage = async (
    userMessage: string,
    messages: Message[],
    onMessageComplete: (aiMessage: Message) => void,
    conversationId?: string | null,
    images?: ImageAttachment[],
    documents?: DocumentAttachment[]
  ) => {
    // Abort any previous in-flight stream before starting a new one.
    abortControllerRef.current?.abort();
    const controller = new AbortController();
    abortControllerRef.current = controller;

    setIsStreaming(true);
    setStreamingMessage('');

    // Build conversation history
    const conversationHistory = messages
      .slice(-10) // Last 10 messages for context
      .map((msg) => msg.content);

    let fullResponse = '';

    // Only apply React state updates if this stream is still the active one and
    // the component is still mounted (prevents post-abort/unmount updates).
    const isActive = () => mountedRef.current && abortControllerRef.current === controller;

    await chatService.streamMessage(
      {
        message: userMessage,
        conversationHistory,
        conversationId: conversationId || undefined,
        images,
        documents,
      },
      // onChunk
      (content: string) => {
        fullResponse += content;
        if (isActive()) {
          setStreamingMessage(fullResponse);
        }
      },
      // onComplete
      (citations) => {
        if (!isActive()) {
          return;
        }
        const aiMessage: Message = {
          id: Date.now().toString(),
          role: 'assistant',
          content: fullResponse,
          timestamp: new Date(),
          citations,
        };
        onMessageComplete(aiMessage);
        abortControllerRef.current = null;
        setIsStreaming(false);
        setStreamingMessage('');
      },
      // onError
      (error: Error) => {
        console.error('Streaming error:', error);
        if (!isActive()) {
          return;
        }

        const status = error instanceof StreamError ? error.status : undefined;
        const isAuthError = status === 401 || status === 403;
        const content = isAuthError
          ? 'Your session has expired or you are not authorized. Please log in again.'
          : `Sorry, an error occurred${error.message ? `: ${error.message}` : ''}. Please try again.`;

        const errorMessage: Message = {
          id: Date.now().toString(),
          role: 'assistant',
          content,
          timestamp: new Date(),
        };
        onMessageComplete(errorMessage);
        abortControllerRef.current = null;
        setIsStreaming(false);
        setStreamingMessage('');
      },
      controller.signal
    );
  };

  return {
    isStreaming,
    streamingMessage,
    sendStreamingMessage,
    stop,
  };
}
