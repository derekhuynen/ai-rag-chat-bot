import { beforeEach, describe, expect, it } from 'vitest';
import { storageService } from './storage';

const TOKEN_KEY = 'auth_token';

describe('storageService', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('returns null when no token is stored', () => {
    expect(storageService.getToken()).toBeNull();
  });

  it('stores a persistent token in localStorage when rememberMe is true', () => {
    storageService.setToken('abc123', true);

    expect(localStorage.getItem(TOKEN_KEY)).toBe('abc123');
    expect(sessionStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(storageService.getToken()).toBe('abc123');
  });

  it('stores a session-only token in sessionStorage by default', () => {
    storageService.setToken('session-token');

    expect(sessionStorage.getItem(TOKEN_KEY)).toBe('session-token');
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(storageService.getToken()).toBe('session-token');
  });

  it('switching to rememberMe clears the prior session token', () => {
    storageService.setToken('session-token');
    storageService.setToken('persistent-token', true);

    expect(localStorage.getItem(TOKEN_KEY)).toBe('persistent-token');
    expect(sessionStorage.getItem(TOKEN_KEY)).toBeNull();
  });

  it('switching from rememberMe to session clears the prior persistent token', () => {
    storageService.setToken('persistent-token', true);
    storageService.setToken('session-token');

    expect(sessionStorage.getItem(TOKEN_KEY)).toBe('session-token');
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
  });

  it('prefers the localStorage token over sessionStorage when both are present', () => {
    localStorage.setItem(TOKEN_KEY, 'local');
    sessionStorage.setItem(TOKEN_KEY, 'session');

    expect(storageService.getToken()).toBe('local');
  });

  it('removeToken clears the token from both storages', () => {
    localStorage.setItem(TOKEN_KEY, 'local');
    sessionStorage.setItem(TOKEN_KEY, 'session');

    storageService.removeToken();

    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(sessionStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(storageService.getToken()).toBeNull();
  });

  it('clearAll wipes unrelated keys too', () => {
    localStorage.setItem('other', 'value');
    sessionStorage.setItem('other', 'value');
    storageService.setToken('persistent', true);

    storageService.clearAll();

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });
});
