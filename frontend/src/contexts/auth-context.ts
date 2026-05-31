import { createContext } from 'react';
import type { User, LoginRequest, RegisterRequest } from '../types/auth';

export interface AuthContextType {
    user: User | null;
    isLoading: boolean;
    login: (credentials: LoginRequest, rememberMe?: boolean) => Promise<void>;
    register: (credentials: RegisterRequest) => Promise<void>;
    logout: () => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);
