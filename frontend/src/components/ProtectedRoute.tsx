import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { Box, CircularProgress } from '@mui/material';
import { useAuth } from '../hooks/useAuth';

interface ProtectedRouteProps {
    children: ReactNode;
    /** When true, only users with the Admin role may proceed. */
    requireAdmin?: boolean;
}

/**
 * Route guard that waits for auth loading to resolve before deciding, so the
 * wrapped page (and its data-fetching effects) never mount for an unauthorized
 * user. Unauthenticated users are sent to login; authenticated-but-non-Admin
 * users are sent back to the home page when an admin route is requested.
 *
 * The login UI lives on the home route ("/"), so redirects target "/", and there
 * is no standalone "/login" page in this app.
 */
export function ProtectedRoute({ children, requireAdmin = false }: ProtectedRouteProps) {
    const { user, isLoading } = useAuth();

    // Wait for the initial session restore to finish before deciding, otherwise
    // a logged-in user would be bounced on a hard refresh.
    if (isLoading) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="100vh">
                <CircularProgress />
            </Box>
        );
    }

    if (!user) {
        return <Navigate to="/" replace />;
    }

    if (requireAdmin && user.role !== 'Admin') {
        return <Navigate to="/" replace />;
    }

    return <>{children}</>;
}

export default ProtectedRoute;
