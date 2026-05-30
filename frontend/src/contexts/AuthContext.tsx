import { useState, useEffect, useCallback, type ReactNode } from 'react';
import type { User, LoginRequest, RegisterRequest } from '../types/auth';
import { authService } from '../services/authService';
import { AUTH_UNAUTHORIZED_EVENT } from '../services/api';
import { AuthContext } from './auth-context';

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const initAuth = async () => {
            const token = authService.getToken();
            if (token) {
                try {
                    const userData = await authService.getMe();
                    setUser(userData);
                } catch {
                    authService.removeToken();
                }
            }
            setIsLoading(false);
        };

        initAuth();
    }, []);

    const login = async (credentials: LoginRequest, rememberMe: boolean = false) => {
        const { token, user: userData } = await authService.login(credentials);
        authService.setToken(token, rememberMe);
        setUser(userData);
    };

    const register = async (credentials: RegisterRequest) => {
        const { token, user: userData } = await authService.register(credentials);
        authService.setToken(token, true); // Always remember on registration
        setUser(userData);
    };

    const logout = useCallback(() => {
        authService.removeToken();
        setUser(null);
    }, []);

    // When the API interceptor sees a 401 (expired/invalid token) it already
    // clears the stored token and fires this event. Drop the in-memory user so
    // protected UI/routes redirect to login without a hard page reload.
    useEffect(() => {
        const handleUnauthorized = () => setUser(null);
        window.addEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);
        return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);
    }, []);

    return (
        <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>
            {children}
        </AuthContext.Provider>
    );
}
