import { useState } from 'react';
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
    CircularProgress,
    Alert,
    Chip,
    Button,
    IconButton,
    Tooltip,
    Stack,
    Breadcrumbs,
    Link,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    DialogActions,
    Snackbar,
} from '@mui/material';
import {
    Delete as DeleteIcon,
    CloudUpload as CloudUploadIcon,
    Download as DownloadIcon,
    Chat as ChatIcon,
    Dashboard as DashboardIcon,
    Article as ArticleIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { documentManagementService, type DocumentListItem, type DocumentStatus } from '../services/documentManagementService';
import { getErrorMessage } from '../utils/errors';

const statusColors: Record<DocumentStatus, 'default' | 'primary' | 'success' | 'warning' | 'error'> = {
    pending: 'warning',
    processing: 'primary',
    processed: 'success',
    failed: 'error',
};

export const AdminDocumentsPage = () => {
    const navigate = useNavigate();
    const { user } = useAuth();
    const queryClient = useQueryClient();
    const [statusFilter, setStatusFilter] = useState<DocumentStatus | undefined>(undefined);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [uploadError, setUploadError] = useState<string | null>(null);
    const [pendingDelete, setPendingDelete] = useState<DocumentListItem | null>(null);
    const [snackbarMessage, setSnackbarMessage] = useState<string | null>(null);

    const isAdmin = user?.role === 'Admin';

    const {
        data,
        isLoading,
        isError,
        error,
    } = useQuery({
        queryKey: ['documents', statusFilter],
        queryFn: () => documentManagementService.listDocuments(statusFilter),
        // Defense-in-depth: never fetch documents for non-admins even if this
        // page were rendered outside the route guard.
        enabled: isAdmin,
    });

    const uploadMutation = useMutation({
        mutationFn: (file: File) => documentManagementService.uploadDocument(file),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['documents'] });
            setSelectedFile(null);
            setUploadError(null);
        },
        onError: (err) => {
            setUploadError(getErrorMessage(err));
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => documentManagementService.deleteDocument(id),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['documents'] });
        },
    });

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) return;

        const lowerName = file.name.toLowerCase();
        if (!lowerName.endsWith('.txt') && !lowerName.endsWith('.md')) {
            setUploadError('Only .txt and .md files are supported');
            setSelectedFile(null);
            return;
        }

        const maxSize = 10 * 1024 * 1024;
        if (file.size > maxSize) {
            setUploadError('File too large. Max size is 10MB');
            setSelectedFile(null);
            return;
        }

        setUploadError(null);
        setSelectedFile(file);
    };

    const handleUpload = () => {
        if (selectedFile) {
            uploadMutation.mutate(selectedFile);
        }
    };

    const handleDelete = (doc: DocumentListItem) => {
        setPendingDelete(doc);
    };

    const handleCancelDelete = () => {
        setPendingDelete(null);
    };

    const handleConfirmDelete = () => {
        if (pendingDelete) {
            deleteMutation.mutate(pendingDelete.id);
        }
        setPendingDelete(null);
    };

    const handleDownload = async (doc: DocumentListItem) => {
        try {
            const info = await documentManagementService.getDownloadInfo(doc.id);
            window.open(info.downloadUrl, '_blank');
        } catch (err) {
            console.error('Failed to get download URL', err);
            setSnackbarMessage('Failed to get download URL');
        }
    };

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleString();
    };

    const formatSize = (bytes: number) => {
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    };

    if (!isAdmin) {
        return (
            <Container maxWidth="lg" sx={{ mt: 4 }}>
                <Alert severity="error">Access denied. You must be an administrator to view this page.</Alert>
            </Container>
        );
    }

    return (
        <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
            <Grid container spacing={3}>
                <Grid size={{ xs: 12, md: 9 }}>
                    <Breadcrumbs aria-label="breadcrumb" sx={{ mb: 2 }}>
                        <Link component={RouterLink} to="/" color="inherit" underline="hover">
                            Home
                        </Link>
                        <Link component={RouterLink} to="/admin" color="inherit" underline="hover">
                            Dashboard
                        </Link>
                        <Typography color="text.primary">Documents</Typography>
                    </Breadcrumbs>

                    <Typography variant="h4" gutterBottom sx={{ mb: 3 }}>
                        Document Library
                    </Typography>

                    {/* Upload Section */}
                    <Paper sx={{ p: 2, mb: 3 }}>
                        <Grid container spacing={2} alignItems="center">
                            <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                                <Button
                                    variant="outlined"
                                    component="label"
                                    startIcon={<CloudUploadIcon />}
                                    disabled={uploadMutation.isPending}
                                >
                                    {selectedFile ? 'Change file' : 'Select .txt or .md document'}
                                    <input
                                        type="file"
                                        hidden
                                        accept=".txt,.md,text/plain,text/markdown"
                                        onChange={handleFileChange}
                                    />
                                </Button>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                                <Typography variant="body2" color="text.secondary">
                                    Plain text (.txt) and Markdown (.md) files up to 10MB are supported.
                                </Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                                <Stack
                                    direction="row"
                                    spacing={2}
                                    justifyContent={{ xs: 'flex-start', md: 'flex-end' }}
                                    alignItems="center"
                                >
                                    {selectedFile && (
                                        <Typography variant="body2" color="text.secondary">
                                            Selected: {selectedFile.name} ({formatSize(selectedFile.size)})
                                        </Typography>
                                    )}
                                    <Button
                                        variant="contained"
                                        onClick={handleUpload}
                                        disabled={!selectedFile || uploadMutation.isPending}
                                    >
                                        {uploadMutation.isPending ? 'Uploading...' : 'Upload & Process'}
                                    </Button>
                                </Stack>
                            </Grid>
                        </Grid>
                        {uploadError && (
                            <Alert severity="error" sx={{ mt: 2 }}>
                                {uploadError}
                            </Alert>
                        )}
                    </Paper>

                    {/* Status Filters */}
                    <Box sx={{ mb: 2, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                        <Chip
                            label="All"
                            clickable
                            onClick={() => setStatusFilter(undefined)}
                            color={statusFilter === undefined ? 'primary' : 'default'}
                            variant={statusFilter === undefined ? 'filled' : 'outlined'}
                        />
                        {(['pending', 'processing', 'processed', 'failed'] as DocumentStatus[]).map((status) => (
                            <Chip
                                key={status}
                                label={status.charAt(0).toUpperCase() + status.slice(1)}
                                clickable
                                onClick={() => setStatusFilter(status)}
                                color={statusFilter === status ? statusColors[status] : 'default'}
                                variant={statusFilter === status ? 'filled' : 'outlined'}
                            />
                        ))}
                    </Box>

                    {/* Documents Table */}
                    <Paper sx={{ width: '100%', overflow: 'hidden' }}>
                        <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
                            <Typography variant="h6">Documents ({data?.total ?? 0})</Typography>
                        </Box>
                        {isLoading ? (
                            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                                <CircularProgress />
                            </Box>
                        ) : isError ? (
                            <Box sx={{ p: 2 }}>
                                <Alert severity="error">{getErrorMessage(error) || 'Failed to load documents'}</Alert>
                            </Box>
                        ) : data && data.documents.length > 0 ? (
                            <TableContainer sx={{ maxHeight: 600 }}>
                                <Table stickyHeader size="small">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Name</TableCell>
                                            <TableCell>Status</TableCell>
                                            <TableCell>Uploaded By</TableCell>
                                            <TableCell>Uploaded At</TableCell>
                                            <TableCell align="right">Size</TableCell>
                                            <TableCell align="right">Chunks</TableCell>
                                            <TableCell align="right">Actions</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {data.documents.map((doc) => (
                                            <TableRow key={doc.id} hover>
                                                <TableCell>
                                                    <Typography variant="body2">
                                                        {doc.originalFileName || doc.fileName}
                                                    </Typography>
                                                    {doc.processingError && (
                                                        <Typography variant="caption" color="error">
                                                            {doc.processingError}
                                                        </Typography>
                                                    )}
                                                </TableCell>
                                                <TableCell>
                                                    <Chip
                                                        label={doc.status.charAt(0).toUpperCase() + doc.status.slice(1)}
                                                        color={statusColors[doc.status]}
                                                        size="small"
                                                    />
                                                </TableCell>
                                                <TableCell>{doc.uploadedBy}</TableCell>
                                                <TableCell>{formatDate(doc.uploadedAt)}</TableCell>
                                                <TableCell align="right">{formatSize(doc.fileSize)}</TableCell>
                                                <TableCell align="right">{doc.totalChunks}</TableCell>
                                                <TableCell align="right">
                                                    <Stack direction="row" spacing={1} justifyContent="flex-end">
                                                        <Tooltip title="Download">
                                                            <span>
                                                                <IconButton
                                                                    size="small"
                                                                    onClick={() => handleDownload(doc)}
                                                                    disabled={doc.status !== 'processed'}
                                                                >
                                                                    <DownloadIcon fontSize="small" />
                                                                </IconButton>
                                                            </span>
                                                        </Tooltip>
                                                        <Tooltip title="Delete">
                                                            <IconButton size="small" onClick={() => handleDelete(doc)}>
                                                                <DeleteIcon fontSize="small" />
                                                            </IconButton>
                                                        </Tooltip>
                                                    </Stack>
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        ) : (
                            <Box sx={{ p: 2 }}>
                                <Typography variant="body2" color="text.secondary">
                                    No documents found.
                                </Typography>
                            </Box>
                        )}
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
                            variant="outlined"
                            startIcon={<DashboardIcon />}
                            onClick={() => navigate('/admin')}
                            fullWidth
                        >
                            Dashboard
                        </Button>
                        <Button
                            variant="contained"
                            startIcon={<ArticleIcon />}
                            onClick={() => navigate('/admin/documents')}
                            fullWidth
                        >
                            Documents
                        </Button>
                    </Box>
                </Grid>
            </Grid>

            {/* Delete Confirmation Dialog */}
            <Dialog open={pendingDelete !== null} onClose={handleCancelDelete}>
                <DialogTitle>Delete document?</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        {pendingDelete
                            ? `Delete document "${pendingDelete.originalFileName || pendingDelete.fileName}"? This cannot be undone.`
                            : ''}
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCancelDelete}>Cancel</Button>
                    <Button onClick={handleConfirmDelete} color="error" autoFocus>
                        Delete
                    </Button>
                </DialogActions>
            </Dialog>

            <Snackbar
                open={snackbarMessage !== null}
                autoHideDuration={6000}
                onClose={() => setSnackbarMessage(null)}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                <Alert onClose={() => setSnackbarMessage(null)} severity="error" variant="filled" sx={{ width: '100%' }}>
                    {snackbarMessage}
                </Alert>
            </Snackbar>
        </Container>
    );
};
