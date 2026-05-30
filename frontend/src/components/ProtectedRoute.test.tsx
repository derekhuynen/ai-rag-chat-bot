import { describe, expect, it, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import type { User } from '../types/auth';
import { ProtectedRoute } from './ProtectedRoute';

// Mock the auth hook so we can drive each guard branch directly.
const mockUseAuth = vi.fn();
vi.mock('../hooks/useAuth', () => ({
  useAuth: () => mockUseAuth(),
}));

const adminUser: User = {
  id: '1',
  email: 'admin@example.com',
  name: 'Admin',
  role: 'Admin',
  createdAt: '2024-01-01T00:00:00Z',
};

const regularUser: User = { ...adminUser, role: 'User', email: 'user@example.com' };

function renderGuard(requireAdmin = false) {
  return render(
    <MemoryRouter initialEntries={['/protected']}>
      <Routes>
        <Route path="/" element={<div>Home / Login</div>} />
        <Route
          path="/protected"
          element={
            <ProtectedRoute requireAdmin={requireAdmin}>
              <div>Secret Content</div>
            </ProtectedRoute>
          }
        />
      </Routes>
    </MemoryRouter>
  );
}

describe('ProtectedRoute', () => {
  beforeEach(() => {
    mockUseAuth.mockReset();
  });

  it('shows a loading spinner while auth is resolving', () => {
    mockUseAuth.mockReturnValue({ user: null, isLoading: true });
    renderGuard();

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    expect(screen.queryByText('Secret Content')).not.toBeInTheDocument();
    expect(screen.queryByText('Home / Login')).not.toBeInTheDocument();
  });

  it('redirects unauthenticated users to the home/login route', () => {
    mockUseAuth.mockReturnValue({ user: null, isLoading: false });
    renderGuard();

    expect(screen.getByText('Home / Login')).toBeInTheDocument();
    expect(screen.queryByText('Secret Content')).not.toBeInTheDocument();
  });

  it('renders children for an authenticated user on a non-admin route', () => {
    mockUseAuth.mockReturnValue({ user: regularUser, isLoading: false });
    renderGuard(false);

    expect(screen.getByText('Secret Content')).toBeInTheDocument();
  });

  it('redirects a non-admin user away from an admin-only route', () => {
    mockUseAuth.mockReturnValue({ user: regularUser, isLoading: false });
    renderGuard(true);

    expect(screen.getByText('Home / Login')).toBeInTheDocument();
    expect(screen.queryByText('Secret Content')).not.toBeInTheDocument();
  });

  it('renders children for an admin on an admin-only route', () => {
    mockUseAuth.mockReturnValue({ user: adminUser, isLoading: false });
    renderGuard(true);

    expect(screen.getByText('Secret Content')).toBeInTheDocument();
  });
});
