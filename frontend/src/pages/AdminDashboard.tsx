import { useState, useEffect } from 'react';
import {
    Box,
    Container,
    Typography,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Grid,
    Card,
    CardContent,
    CircularProgress,
    Alert,
    Chip,
    Breadcrumbs,
    Link,
    Button,
} from '@mui/material';
import {
    People as PeopleIcon,
    Chat as ChatIcon,
    Message as MessageIcon,
    PersonOutline as PersonOutlineIcon,
    Dashboard as DashboardIcon,
    Article as ArticleIcon,
} from '@mui/icons-material';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { adminService } from '../services/adminService';
import type { AdminUser, AdminStats } from '../services/adminService';
import { useAuth } from '../hooks/useAuth';
import { getErrorMessage } from '../utils/errors';

export const AdminDashboard = () => {
    const navigate = useNavigate();
    const { user } = useAuth();
    const [users, setUsers] = useState<AdminUser[]>([]);
    const [stats, setStats] = useState<AdminStats | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        // Defense-in-depth: only fetch admin data once we know the user is an
        // Admin, so getAllUsers()/getStats() never fire for non-admins even if
        // this component were rendered outside the route guard.
        if (user?.role !== 'Admin') {
            return;
        }

        const fetchData = async () => {
            try {
                setLoading(true);
                setError(null);

                const [usersData, statsData] = await Promise.all([
                    adminService.getAllUsers(),
                    adminService.getStats()
                ]);

                setUsers(usersData);
                setStats(statsData);
            } catch (err) {
                console.error('Error fetching admin data:', err);
                setError(getErrorMessage(err));
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [user?.role]);

    if (user?.role !== 'Admin') {
        return (
            <Container maxWidth="lg" sx={{ mt: 4 }}>
                <Alert severity="error">
                    Access denied. You must be an administrator to view this page.
                </Alert>
            </Container>
        );
    }

    if (loading) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="80vh">
                <CircularProgress />
            </Box>
        );
    }

    if (error) {
        return (
            <Container maxWidth="lg" sx={{ mt: 4 }}>
                <Alert severity="error">{error}</Alert>
            </Container>
        );
    }

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const getRoleColor = (role: string): "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning" => {
        switch (role.toLowerCase()) {
            case 'admin':
                return 'error';
            case 'user':
                return 'primary';
            default:
                return 'default';
        }
    };

    return (
        <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
            <Grid container spacing={3}>
                <Grid size={{ xs: 12, md: 9 }}>
                    <Breadcrumbs aria-label="breadcrumb" sx={{ mb: 2 }}>
                        <Link component={RouterLink} to="/" color="inherit" underline="hover">
                            Home
                        </Link>
                        <Typography color="text.primary">Dashboard</Typography>
                    </Breadcrumbs>

                    <Typography variant="h4" gutterBottom sx={{ mb: 4 }}>
                        Admin Dashboard
                    </Typography>

                    {/* Stats Cards */}
                    {stats && (
                        <Grid container spacing={3} sx={{ mb: 4 }}>
                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <Card>
                                    <CardContent>
                                        <Box display="flex" alignItems="center" justifyContent="space-between">
                                            <Box>
                                                <Typography color="textSecondary" gutterBottom variant="body2">
                                                    Total Users
                                                </Typography>
                                                <Typography variant="h4">
                                                    {stats.totalUsers}
                                                </Typography>
                                            </Box>
                                            <PeopleIcon sx={{ fontSize: 48, color: 'primary.main', opacity: 0.3 }} />
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <Card>
                                    <CardContent>
                                        <Box display="flex" alignItems="center" justifyContent="space-between">
                                            <Box>
                                                <Typography color="textSecondary" gutterBottom variant="body2">
                                                    Conversations
                                                </Typography>
                                                <Typography variant="h4">
                                                    {stats.totalConversations}
                                                </Typography>
                                            </Box>
                                            <ChatIcon sx={{ fontSize: 48, color: 'success.main', opacity: 0.3 }} />
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <Card>
                                    <CardContent>
                                        <Box display="flex" alignItems="center" justifyContent="space-between">
                                            <Box>
                                                <Typography color="textSecondary" gutterBottom variant="body2">
                                                    Total Messages
                                                </Typography>
                                                <Typography variant="h4">
                                                    {stats.totalMessages}
                                                </Typography>
                                            </Box>
                                            <MessageIcon sx={{ fontSize: 48, color: 'info.main', opacity: 0.3 }} />
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                                <Card>
                                    <CardContent>
                                        <Box display="flex" alignItems="center" justifyContent="space-between">
                                            <Box>
                                                <Typography color="textSecondary" gutterBottom variant="body2">
                                                    Active Users
                                                </Typography>
                                                <Typography variant="h4">
                                                    {stats.usersWithConversations}
                                                </Typography>
                                            </Box>
                                            <PersonOutlineIcon sx={{ fontSize: 48, color: 'warning.main', opacity: 0.3 }} />
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>
                        </Grid>
                    )}

                    {/* Users Table */}
                    <Paper sx={{ width: '100%', overflow: 'hidden' }}>
                        <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
                            <Typography variant="h6">
                                All Users ({users.length})
                            </Typography>
                        </Box>
                        <TableContainer sx={{ maxHeight: 600 }}>
                            <Table stickyHeader>
                                <TableHead>
                                    <TableRow>
                                        <TableCell>Name</TableCell>
                                        <TableCell>Email</TableCell>
                                        <TableCell>Role</TableCell>
                                        <TableCell>Created At</TableCell>
                                        <TableCell>User ID</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {users.map((user) => (
                                        <TableRow key={user.id} hover>
                                            <TableCell>{user.name}</TableCell>
                                            <TableCell>{user.email}</TableCell>
                                            <TableCell>
                                                <Chip
                                                    label={user.role}
                                                    color={getRoleColor(user.role)}
                                                    size="small"
                                                />
                                            </TableCell>
                                            <TableCell>{formatDate(user.createdAt)}</TableCell>
                                            <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>
                                                {user.id}
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </Paper>
                </Grid>

                {/* Right-side admin navbar */}
                <Grid size={{ xs: 12, md: 3 }}>
                    <Box
                        sx={{
                            position: { md: 'sticky' },
                            top: 80,
                            mt: { xs: 3, md: 0 },
                            display: 'flex',
                            flexDirection: 'column',
                            gap: 2,
                        }}
                    >
                        <Typography variant="subtitle1" gutterBottom>
                            Admin Navigation
                        </Typography>
                        <Button
                            variant="outlined"
                            startIcon={<ChatIcon />}
                            onClick={() => navigate('/')}
                            fullWidth
                        >
                            Back to Chat
                        </Button>
                        <Button
                            variant="contained"
                            startIcon={<DashboardIcon />}
                            onClick={() => navigate('/admin')}
                            fullWidth
                        >
                            Dashboard
                        </Button>
                        <Button
                            variant="outlined"
                            startIcon={<ArticleIcon />}
                            onClick={() => navigate('/admin/documents')}
                            fullWidth
                        >
                            Documents
                        </Button>
                    </Box>
                </Grid>
            </Grid>
        </Container>
    );
};
