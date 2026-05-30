import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    TextField,
    Button,
    Box,
    Typography,
    Alert,
    FormControlLabel,
    Checkbox,
} from '@mui/material';
import { useAuth } from '../hooks/useAuth';
import { getErrorMessage } from '../utils/errors';
import { darkTextFieldStyles, darkDialogStyles, darkButtonStyles, darkCheckboxStyles, colors } from '../utils/theme';
import type { LoginRequest } from '../types/auth';

const loginSchema = z.object({
    email: z.string().email('Invalid email address'),
    password: z.string().min(6, 'Password must be at least 6 characters'),
});

interface LoginModalProps {
    open: boolean;
    onClose: () => void;
    onSwitchToRegister: () => void;
}

export default function LoginModal({ open, onClose, onSwitchToRegister }: LoginModalProps) {
    const { login } = useAuth();
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [rememberMe, setRememberMe] = useState(false);

    const {
        register,
        handleSubmit,
        formState: { errors },
        reset,
    } = useForm<LoginRequest>({
        resolver: zodResolver(loginSchema),
    });

    const onSubmit = async (data: LoginRequest) => {
        setError('');
        setIsLoading(true);
        try {
            await login(data, rememberMe);
            reset();
            onClose();
        } catch (err) {
            setError(getErrorMessage(err));
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
            <DialogTitle sx={darkDialogStyles.title}>Sign In</DialogTitle>
            <DialogContent sx={darkDialogStyles.content}>
                <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ mt: 1 }}>
                    {error && (
                        <Alert severity="error" sx={{ mb: 2 }}>
                            {error}
                        </Alert>
                    )}

                    <TextField
                        {...register('email')}
                        margin="normal"
                        fullWidth
                        label="Email Address"
                        autoComplete="email"
                        autoFocus
                        error={!!errors.email}
                        helperText={errors.email?.message}
                        sx={darkTextFieldStyles}
                    />

                    <TextField
                        {...register('password')}
                        margin="normal"
                        fullWidth
                        label="Password"
                        type="password"
                        autoComplete="current-password"
                        error={!!errors.password}
                        helperText={errors.password?.message}
                        sx={darkTextFieldStyles}
                    />

                    <FormControlLabel
                        control={
                            <Checkbox
                                checked={rememberMe}
                                onChange={(e) => setRememberMe(e.target.checked)}
                                sx={{
                                    color: colors.text.secondary,
                                    ...darkCheckboxStyles,
                                }}
                            />
                        }
                        label="Remember me"
                        sx={{
                            color: colors.text.secondary,
                            mt: 1,
                            '& .MuiFormControlLabel-label': { fontSize: '0.875rem' },
                        }}
                    />

                    <Button
                        type="submit"
                        fullWidth
                        variant="contained"
                        disabled={isLoading}
                        sx={{
                            mt: 3,
                            mb: 2,
                            ...darkButtonStyles.primary,
                        }}
                    >
                        {isLoading ? 'Signing in...' : 'Sign In'}
                    </Button>

                    <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="body2" sx={{ color: colors.text.secondary }}>
                            Don't have an account?{' '}
                            <Button
                                onClick={onSwitchToRegister}
                                sx={{
                                    color: colors.button.primary,
                                    textTransform: 'none',
                                    p: 0,
                                    minWidth: 'auto',
                                    '&:hover': { bgcolor: 'transparent', textDecoration: 'underline' },
                                }}
                            >
                                Sign up
                            </Button>
                        </Typography>
                    </Box>
                </Box>
            </DialogContent>
        </Dialog>
    );
}
