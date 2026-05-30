import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { chatService, StreamError } from './chatService';
import type { ChatRequest } from '../types';

const request: ChatRequest = { message: 'hello' };

const encoder = new TextEncoder();

/**
 * Build a Response whose body is a ReadableStream that emits the given chunks
 * (already strings) one at a time. This lets each test control exactly where
 * SSE event boundaries fall relative to chunk boundaries.
 */
function streamingResponse(chunks: string[], init?: Partial<{ ok: boolean; status: number }>): Response {
  const body = new ReadableStream<Uint8Array>({
    start(controller) {
      for (const chunk of chunks) {
        controller.enqueue(encoder.encode(chunk));
      }
      controller.close();
    },
  });

  return {
    ok: init?.ok ?? true,
    status: init?.status ?? 200,
    statusText: 'OK',
    body,
    text: async () => '',
  } as unknown as Response;
}

function runStream(response: Response) {
  const onChunk = vi.fn();
  const onComplete = vi.fn();
  const onError = vi.fn();
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response));
  return { onChunk, onComplete, onError };
}

describe('chatService.streamMessage SSE parsing', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('reassembles content chunks delivered as separate complete events', async () => {
    const { onChunk, onComplete, onError } = runStream(
      streamingResponse([
        'data: {"content":"Hello "}\n\n',
        'data: {"content":"world"}\n\n',
        'data: [DONE]\n\n',
      ])
    );

    await chatService.streamMessage(request, onChunk, onComplete, onError);

    expect(onError).not.toHaveBeenCalled();
    expect(onChunk.mock.calls.map((c) => c[0])).toEqual(['Hello ', 'world']);
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('does not drop an event that is split across two read chunks', async () => {
    const { onChunk, onComplete, onError } = runStream(
      streamingResponse([
        // First read ends mid-event (no trailing blank line yet).
        'data: {"content":"Str',
        // Second read completes the event and adds another full event.
        'eaming"}\n\ndata: {"content":" rocks"}\n\ndata: [DONE]\n\n',
      ])
    );

    await chatService.streamMessage(request, onChunk, onComplete, onError);

    expect(onError).not.toHaveBeenCalled();
    expect(onChunk.mock.calls.map((c) => c[0])).toEqual(['Streaming', ' rocks']);
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('flushes a trailing event that arrives without a final blank-line terminator', async () => {
    const { onChunk, onComplete, onError } = runStream(
      streamingResponse([
        'data: {"content":"first"}\n\n',
        // No trailing "\n\n";relies on the post-loop flush.
        'data: {"content":"last"}',
      ])
    );

    await chatService.streamMessage(request, onChunk, onComplete, onError);

    expect(onError).not.toHaveBeenCalled();
    expect(onChunk.mock.calls.map((c) => c[0])).toEqual(['first', 'last']);
    // Stream ended without [DONE];completion is still signaled once.
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('captures citations and passes them to onComplete on [DONE]', async () => {
    const citations = [{ id: 'doc1', title: 'Doc One' }];
    const { onChunk, onComplete, onError } = runStream(
      streamingResponse([
        `data: ${JSON.stringify({ citations })}\n\n`,
        'data: [DONE]\n\n',
      ])
    );

    await chatService.streamMessage(request, onChunk, onComplete, onError);

    expect(onError).not.toHaveBeenCalled();
    expect(onComplete).toHaveBeenCalledWith(citations);
  });

  it('surfaces a server error event via onError', async () => {
    const { onChunk, onComplete, onError } = runStream(
      streamingResponse(['data: {"error":"model exploded"}\n\n'])
    );

    await chatService.streamMessage(request, onChunk, onComplete, onError);

    expect(onComplete).not.toHaveBeenCalled();
    expect(onError).toHaveBeenCalledTimes(1);
    const err = onError.mock.calls[0][0] as StreamError;
    expect(err).toBeInstanceOf(StreamError);
    expect(err.message).toBe('model exploded');
  });

  it('ignores non-JSON / malformed data lines without erroring', async () => {
    const { onChunk, onComplete, onError } = runStream(
      streamingResponse([
        'data: not-json-at-all\n\n',
        'data: {"content":"ok"}\n\n',
        'data: [DONE]\n\n',
      ])
    );

    await chatService.streamMessage(request, onChunk, onComplete, onError);

    expect(onError).not.toHaveBeenCalled();
    expect(onChunk.mock.calls.map((c) => c[0])).toEqual(['ok']);
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('reports an HTTP error response as a StreamError carrying the status', async () => {
    const errorResponse = {
      ok: false,
      status: 401,
      statusText: 'Unauthorized',
      body: null,
      text: async () => 'token expired',
    } as unknown as Response;
    const { onChunk, onComplete, onError } = runStream(errorResponse);

    await chatService.streamMessage(request, onChunk, onComplete, onError);

    expect(onComplete).not.toHaveBeenCalled();
    expect(onError).toHaveBeenCalledTimes(1);
    const err = onError.mock.calls[0][0] as StreamError;
    expect(err).toBeInstanceOf(StreamError);
    expect(err.status).toBe(401);
    expect(err.message).toBe('token expired');
  });

  it('swallows AbortError (intentional cancellation) without calling onError', async () => {
    const abortError = new DOMException('aborted', 'AbortError');
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(abortError));
    const onChunk = vi.fn();
    const onComplete = vi.fn();
    const onError = vi.fn();

    await chatService.streamMessage(request, onChunk, onComplete, onError);

    expect(onError).not.toHaveBeenCalled();
    expect(onComplete).not.toHaveBeenCalled();
  });

  it('sends the bearer token from storage when present', async () => {
    localStorage.setItem('auth_token', 'tok-123');
    const fetchMock = vi.fn().mockResolvedValue(streamingResponse(['data: [DONE]\n\n']));
    vi.stubGlobal('fetch', fetchMock);

    await chatService.streamMessage(request, vi.fn(), vi.fn(), vi.fn());

    const [, options] = fetchMock.mock.calls[0];
    expect(options.headers.Authorization).toBe('Bearer tok-123');
    expect(options.method).toBe('POST');
    expect(JSON.parse(options.body).useRAG).toBe(true);
  });
});
