import { Box, TextField, IconButton, ImageList, ImageListItem, ImageListItemBar, List, ListItem, ListItemText, ListItemIcon, Snackbar, Alert } from '@mui/material';
import SendIcon from '@mui/icons-material/Send';
import AttachFileIcon from '@mui/icons-material/AttachFile';
import DescriptionIcon from '@mui/icons-material/Description';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import TextSnippetIcon from '@mui/icons-material/TextSnippet';
import ArticleIcon from '@mui/icons-material/Article';
import CloseIcon from '@mui/icons-material/Close';
import { useState, useRef, type KeyboardEvent, type DragEvent, type ClipboardEvent } from 'react';
import { imageUploadService } from '../services/imageUploadService';
import { documentUploadService } from '../services/documentUploadService';
import type { ImageAttachment, DocumentAttachment } from '../types/chat';

interface MessageInputProps {
    onSendMessage: (message: string, images?: ImageAttachment[], documents?: DocumentAttachment[]) => void;
    disabled?: boolean;
    conversationId?: string;
}

function MessageInput({ onSendMessage, disabled = false, conversationId }: MessageInputProps) {
    // const theme = useTheme(); // TODO: Apply theme colors to MessageInput
    const [message, setMessage] = useState('');
    const [selectedImages, setSelectedImages] = useState<File[]>([]);
    const [imagePreviews, setImagePreviews] = useState<string[]>([]);
    const [selectedDocuments, setSelectedDocuments] = useState<File[]>([]);
    const [uploading, setUploading] = useState(false);
    const [dragActive, setDragActive] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const showError = (msg: string) => setErrorMessage(msg);
    const handleCloseError = () => setErrorMessage(null);

    const handleSend = async () => {
        if ((message.trim() || selectedImages.length > 0 || selectedDocuments.length > 0) && !disabled && !uploading) {
            setUploading(true);
            try {
                let uploadedImages: ImageAttachment[] | undefined;
                let uploadedDocuments: DocumentAttachment[] | undefined;

                // Upload images if any are selected
                if (selectedImages.length > 0) {
                    uploadedImages = await imageUploadService.uploadImages(selectedImages, conversationId);
                }

                // Upload documents if any are selected
                if (selectedDocuments.length > 0) {
                    uploadedDocuments = await documentUploadService.uploadDocuments(selectedDocuments, conversationId);
                }

                // Send message with images and documents
                onSendMessage(message.trim(), uploadedImages, uploadedDocuments);

                // Clear state
                setMessage('');
                clearImages();
                clearDocuments();
            } catch (error) {
                console.error('Error uploading files:', error);
                showError('Failed to upload files. Please try again.');
            } finally {
                setUploading(false);
            }
        }
    };

    const handleFileSelect = (files: FileList | null) => {
        if (!files) return;

        const fileArray = Array.from(files);
        const imageFiles: File[] = [];
        const documentFiles: File[] = [];

        // Sort files by type
        fileArray.forEach(file => {
            const extension = file.name.toLowerCase().slice(file.name.lastIndexOf('.'));
            const isImage = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.bmp'].includes(extension);
            const isDocument = ['.pdf', '.txt', '.doc', '.docx'].includes(extension);

            if (isImage) {
                imageFiles.push(file);
            } else if (isDocument) {
                documentFiles.push(file);
            }
        });

        // Validate and add images
        if (imageFiles.length > 0) {
            const imageValidation = imageUploadService.validateFiles(imageFiles);
            if (!imageValidation.valid) {
                showError(imageValidation.error || 'Invalid image file.');
                return;
            }

            const newImages = [...selectedImages, ...imageFiles].slice(0, 10);
            setSelectedImages(newImages);

            // Generate preview URLs only for new images
            const newPreviews = [...imagePreviews];
            imageFiles.forEach(file => {
                if (newPreviews.length < 10) {
                    newPreviews.push(URL.createObjectURL(file));
                }
            });
            setImagePreviews(newPreviews);
        }

        // Validate and add documents
        if (documentFiles.length > 0) {
            const docValidation = documentUploadService.validateFiles(documentFiles);
            if (!docValidation.valid) {
                showError(docValidation.error || 'Invalid document file.');
                return;
            }

            const newDocuments = [...selectedDocuments, ...documentFiles].slice(0, 5);
            setSelectedDocuments(newDocuments);
        }
    };

    const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        handleFileSelect(e.target.files);
    };

    const handleAttachClick = () => {
        fileInputRef.current?.click();
    };

    const handleRemoveImage = (index: number) => {
        const newFiles = selectedImages.filter((_, i) => i !== index);
        const newPreviews = imagePreviews.filter((_, i) => i !== index);

        // Revoke URL to free memory
        URL.revokeObjectURL(imagePreviews[index]);

        setSelectedImages(newFiles);
        setImagePreviews(newPreviews);
    };

    const clearImages = () => {
        // Revoke all preview URLs
        imagePreviews.forEach(url => URL.revokeObjectURL(url));
        setSelectedImages([]);
        setImagePreviews([]);
        // Reset file input to allow selecting the same file again
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const handleRemoveDocument = (index: number) => {
        const newFiles = selectedDocuments.filter((_, i) => i !== index);
        setSelectedDocuments(newFiles);
    };

    const clearDocuments = () => {
        setSelectedDocuments([]);
        // Reset file input to allow selecting the same file again
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const getDocumentIcon = (filename: string) => {
        const extension = filename.toLowerCase().slice(filename.lastIndexOf('.'));
        switch (extension) {
            case '.pdf':
                return <PictureAsPdfIcon />;
            case '.txt':
                return <TextSnippetIcon />;
            case '.doc':
            case '.docx':
                return <ArticleIcon />;
            default:
                return <DescriptionIcon />;
        }
    };

    const handlePaste = (e: ClipboardEvent<HTMLDivElement>) => {
        const items = e.clipboardData?.items;
        if (!items) return;

        const imageFiles: File[] = [];
        for (let i = 0; i < items.length; i++) {
            if (items[i].type.startsWith('image/')) {
                const file = items[i].getAsFile();
                if (file) imageFiles.push(file);
            }
        }

        if (imageFiles.length > 0) {
            e.preventDefault();
            const dataTransfer = new DataTransfer();
            imageFiles.forEach(file => dataTransfer.items.add(file));
            handleFileSelect(dataTransfer.files);
        }
    };

    const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        e.stopPropagation();
        setDragActive(true);
    };

    const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        e.stopPropagation();
        setDragActive(false);
    };

    const handleDrop = (e: DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        e.stopPropagation();
        setDragActive(false);

        handleFileSelect(e.dataTransfer.files);
    };

    const formatFileSize = (bytes: number): string => {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    };

    const handleKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    return (
        <Box
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            sx={{
                display: 'flex',
                flexDirection: 'column',
                gap: 1,
                bgcolor: '#40414f',
                borderRadius: 2,
                p: 2,
                boxShadow: '0 0 15px rgba(0,0,0,.1)',
                border: dragActive ? '2px dashed #19c37d' : '2px solid transparent',
                transition: 'border 0.2s',
            }}
        >
            {/* Image preview grid */}
            {imagePreviews.length > 0 && (
                <ImageList
                    cols={5}
                    rowHeight={100}
                    gap={8}
                    sx={{ margin: 0 }}
                >
                    {imagePreviews.map((preview, index) => (
                        <ImageListItem key={index}>
                            <img
                                src={preview}
                                alt={selectedImages[index].name}
                                loading="lazy"
                                style={{ objectFit: 'cover', height: '100%' }}
                            />
                            <ImageListItemBar
                                title={selectedImages[index].name}
                                subtitle={formatFileSize(selectedImages[index].size)}
                                actionIcon={
                                    <IconButton
                                        onClick={() => handleRemoveImage(index)}
                                        sx={{ color: 'white' }}
                                        size="small"
                                    >
                                        <CloseIcon />
                                    </IconButton>
                                }
                                sx={{
                                    background: 'linear-gradient(to top, rgba(0,0,0,0.7) 0%, rgba(0,0,0,0.3) 70%, rgba(0,0,0,0) 100%)',
                                    '& .MuiImageListItemBar-title': {
                                        fontSize: '0.75rem',
                                        overflow: 'hidden',
                                        textOverflow: 'ellipsis',
                                        whiteSpace: 'nowrap',
                                    },
                                    '& .MuiImageListItemBar-subtitle': {
                                        fontSize: '0.7rem',
                                    },
                                }}
                            />
                        </ImageListItem>
                    ))}
                </ImageList>
            )}

            {/* Document list */}
            {selectedDocuments.length > 0 && (
                <List sx={{ padding: 0, margin: 0 }}>
                    {selectedDocuments.map((doc, index) => (
                        <ListItem
                            key={index}
                            sx={{
                                bgcolor: 'rgba(255, 255, 255, 0.05)',
                                borderRadius: 1,
                                mb: 0.5,
                                py: 1,
                            }}
                            secondaryAction={
                                <IconButton
                                    edge="end"
                                    onClick={() => handleRemoveDocument(index)}
                                    sx={{ color: 'rgba(255, 255, 255, 0.7)' }}
                                    size="small"
                                >
                                    <CloseIcon />
                                </IconButton>
                            }
                        >
                            <ListItemIcon sx={{ color: 'rgba(255, 255, 255, 0.7)', minWidth: 40 }}>
                                {getDocumentIcon(doc.name)}
                            </ListItemIcon>
                            <ListItemText
                                primary={doc.name}
                                secondary={formatFileSize(doc.size)}
                                primaryTypographyProps={{
                                    sx: { color: 'white', fontSize: '0.9rem' }
                                }}
                                secondaryTypographyProps={{
                                    sx: { color: 'rgba(255, 255, 255, 0.5)', fontSize: '0.75rem' }
                                }}
                            />
                        </ListItem>
                    ))}
                </List>
            )}

            {/* Input area */}
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-end' }}>
                <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/*,.pdf,.txt,.doc,.docx,application/pdf,text/plain,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    multiple
                    onChange={handleFileInputChange}
                    style={{ display: 'none' }}
                />
                <IconButton
                    onClick={handleAttachClick}
                    disabled={disabled || uploading || (selectedImages.length >= 10 && selectedDocuments.length >= 5)}
                    sx={{
                        color: 'rgba(255, 255, 255, 0.7)',
                        '&:hover': {
                            bgcolor: 'rgba(255, 255, 255, 0.1)',
                        },
                        '&.Mui-disabled': {
                            color: 'rgba(255, 255, 255, 0.3)',
                        },
                    }}
                >
                    <AttachFileIcon />
                </IconButton>
                <TextField
                    fullWidth
                    multiline
                    maxRows={4}
                    placeholder="Send a message..."
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    onKeyDown={handleKeyDown}
                    onPaste={handlePaste}
                    disabled={disabled || uploading}
                    variant="standard"
                    InputProps={{
                        disableUnderline: true,
                        sx: {
                            color: 'white',
                            fontSize: '1rem',
                            '& textarea::placeholder': {
                                color: 'rgba(255, 255, 255, 0.5)',
                            },
                        },
                    }}
                />
                <IconButton
                    onClick={handleSend}
                    disabled={disabled || uploading || !message.trim()}
                    sx={{
                        bgcolor: (!disabled && !uploading && message.trim())
                            ? '#19c37d'
                            : 'rgba(255, 255, 255, 0.1)',
                        color: 'white',
                        '&:hover': {
                            bgcolor: (!disabled && !uploading && message.trim())
                                ? '#15a869'
                                : 'rgba(255, 255, 255, 0.1)',
                        },
                        '&.Mui-disabled': {
                            color: 'rgba(255, 255, 255, 0.3)',
                        },
                    }}
                >
                    <SendIcon />
                </IconButton>
            </Box>

            <Snackbar
                open={errorMessage !== null}
                autoHideDuration={6000}
                onClose={handleCloseError}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                <Alert onClose={handleCloseError} severity="error" variant="filled" sx={{ width: '100%' }}>
                    {errorMessage}
                </Alert>
            </Snackbar>
        </Box>
    );
}

export default MessageInput;
