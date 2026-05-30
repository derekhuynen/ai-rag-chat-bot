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
} from '@mui/material';
import { useAuth } from '../hooks/useAuth';
import { getErrorMessage } from '../utils/errors';
import { darkTextFieldStyles, darkDialogStyles, darkButtonStyles, colors } from '../utils/theme';

const registerSchema = z.object({
    name: z.string().min(1, 'Name is required'),
    email: z.string().email('Invalid email address'),
    password: z.string().min(6, 'Password must be at least 6 characters'),
});

type RegisterFormData = z.infer<typeof registerSchema>;

interface RegisterModalProps {
    open: boolean;
    onClose: () => void;
    onSwitchToLogin: () => void;
}

export default function RegisterModal({ open, onClose, onSwitchToLogin }: RegisterModalProps) {
    const { register: registerUser } = useAuth();
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const {
        register,
        handleSubmit,
        formState: { errors },
        reset,
    } = useForm<RegisterFormData>({
        resolver: zodResolver(registerSchema),
    });

    const onSubmit = async (data: RegisterFormData) => {
        setError('');
        setIsLoading(true);
        try {
            await registerUser(data);
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
            <DialogTitle sx={darkDialogStyles.title}>Create Account</DialogTitle>
            <DialogContent sx={darkDialogStyles.content}>
                <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ mt: 1 }}>
                    {error && (
                        <Alert severity="error" sx={{ mb: 2 }}>
                            {error}
                        </Alert>
                    )}

                    <TextField
                        {...register('name')}
                        margin="normal"
                        fullWidth
                        label="Name"
                        autoComplete="name"
                        autoFocus
                        error={!!errors.name}
                        helperText={errors.name?.message}
                        sx={darkTextFieldStyles}
                    />

                    <TextField
                        {...register('email')}
                        margin="normal"
                        fullWidth
                        label="Email Address"
                        autoComplete="email"
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
                        autoComplete="new-password"
                        error={!!errors.password}
                        helperText={errors.password?.message}
                        sx={darkTextFieldStyles}
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
                        {isLoading ? 'Creating account...' : 'Create Account'}
                    </Button>

                    <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="body2" sx={{ color: colors.text.secondary }}>
                            Already have an account?{' '}
                            <Button
                                onClick={onSwitchToLogin}
                                sx={{
                                    color: colors.button.primary,
                                    textTransform: 'none',
                                    p: 0,
                                    minWidth: 'auto',
                                    '&:hover': { bgcolor: 'transparent', textDecoration: 'underline' },
                                }}
                            >
                                Sign in
                            </Button>
                        </Typography>
                    </Box>
                </Box>
            </DialogContent>
        </Dialog>
    );
}
