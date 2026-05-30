import { describe, expect, it } from 'vitest';
import { getErrorMessage, handleAsyncError } from './errors';

describe('getErrorMessage', () => {
  it('returns a generic message for null/undefined', () => {
    expect(getErrorMessage(null)).toBe('An unknown error occurred');
    expect(getErrorMessage(undefined)).toBe('An unknown error occurred');
  });

  it('extracts the message from a native Error', () => {
    expect(getErrorMessage(new Error('boom'))).toBe('boom');
  });

  it('prefers response.data.error (our API format) over everything else', () => {
    const axiosLike = {
      message: 'Request failed with status code 400',
      response: { data: { error: 'Invalid credentials', message: 'fallback' } },
    };
    expect(getErrorMessage(axiosLike)).toBe('Invalid credentials');
  });

  it('falls back to response.data.message when error is absent', () => {
    const axiosLike = {
      message: 'Request failed with status code 500',
      response: { data: { message: 'Server exploded' } },
    };
    expect(getErrorMessage(axiosLike)).toBe('Server exploded');
  });

  it('falls back to the top-level message when there is no response body', () => {
    const axiosLike = {
      message: 'Network Error',
      response: { data: {} },
    };
    expect(getErrorMessage(axiosLike)).toBe('Network Error');
  });

  it('returns a string error verbatim', () => {
    expect(getErrorMessage('something broke')).toBe('something broke');
  });

  it('returns a generic message for unknown non-object values', () => {
    expect(getErrorMessage(42)).toBe('An unexpected error occurred');
    expect(getErrorMessage(true)).toBe('An unexpected error occurred');
  });

  it('returns a generic message for an empty object', () => {
    expect(getErrorMessage({})).toBe('An unexpected error occurred');
  });
});

describe('handleAsyncError', () => {
  it('returns [null, data] when the promise resolves', async () => {
    const [err, data] = await handleAsyncError(Promise.resolve('ok'));
    expect(err).toBeNull();
    expect(data).toBe('ok');
  });

  it('returns [Error, null] with an extracted message when the promise rejects', async () => {
    const rejected = Promise.reject({ response: { data: { error: 'nope' } } });
    const [err, data] = await handleAsyncError(rejected);
    expect(err).toBeInstanceOf(Error);
    expect(err?.message).toBe('nope');
    expect(data).toBeNull();
  });

  it('uses the fallback message when the rejection yields no message', async () => {
    const [err] = await handleAsyncError(Promise.reject({}), 'Operation failed');
    expect(err?.message).toBe('An unexpected error occurred');
  });
});
