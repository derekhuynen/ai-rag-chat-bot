import { Box, Avatar, Typography, ImageList, ImageListItem, List, ListItem, ListItemIcon, ListItemText, IconButton, Tooltip, useTheme, Chip, Stack, Link as MuiLink } from '@mui/material';
import { SmartToy as BotIcon, Person as PersonIcon, PictureAsPdf, TextSnippet, Article, Description, OpenInNew, ContentCopy, Refresh } from '@mui/icons-material';
import { useState } from 'react';
import { getAvatarColors } from '../theme/theme';
import { useThemeMode } from '../contexts/theme-context';
import type { Message } from '../types';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface MessageItemProps {
    message: Message;
    onRetry?: () => void;
}

function MessageItem({ message, onRetry }: MessageItemProps) {
    const isUser = message.role === 'user';
    const [copied, setCopied] = useState(false);
    const theme = useTheme();
    const { mode } = useThemeMode();

    const handleCopy = async () => {
        try {
            await navigator.clipboard.writeText(message.content);
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        } catch (err) {
            console.error('Failed to copy:', err);
        }
    };

    const handleImageClick = (imageUrl: string) => {
        window.open(imageUrl, '_blank');
    };

    const getDocumentIcon = (filename: string) => {
        const extension = filename.toLowerCase().slice(filename.lastIndexOf('.'));
        switch (extension) {
            case '.pdf':
                return <PictureAsPdf />;
            case '.txt':
                return <TextSnippet />;
            case '.doc':
            case '.docx':
                return <Article />;
            default:
                return <Description />;
        }
    };

    const formatFileSize = (bytes: number): string => {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    };

    return (
        <Box
            sx={{
                py: 3,
                bgcolor: theme.palette.background.default,
                width: '100%',
            }}
        >
            <Box sx={{
                display: 'flex',
                gap: 3,
                maxWidth: '64rem',
                mx: 'auto',
                px: 3,
                flexDirection: isUser ? 'row-reverse' : 'row',
            }}>
                <Avatar
                    sx={{
                        width: 30,
                        height: 30,
                        bgcolor: getAvatarColors(isUser ? 'user' : 'assistant', mode),
                        flexShrink: 0,
                    }}
                >
                    {isUser ? <PersonIcon sx={{ fontSize: 20 }} /> : <BotIcon sx={{ fontSize: 20 }} />}
                </Avatar>

                <Box sx={{ flex: 1, minWidth: 0, textAlign: isUser ? 'right' : 'left' }}>
                    {/* Display images if present */}
                    {message.images && message.images.length > 0 && (
                        <ImageList
                            cols={Math.min(message.images.length, 3)}
                            gap={8}
                            sx={{ mb: 2, overflow: 'hidden' }}
                            rowHeight={200}
                        >
                            {message.images.map((image, index) => (
                                <ImageListItem
                                    key={image.id || index}
                                    sx={{
                                        cursor: 'pointer',
                                        borderRadius: 1,
                                        overflow: 'hidden',
                                        '&:hover': {
                                            opacity: 0.8,
                                        },
                                    }}
                                    onClick={() => handleImageClick(image.url)}
                                >
                                    <img
                                        src={image.thumbnailUrl || image.url}
                                        alt={image.filename}
                                        loading="lazy"
                                        style={{
                                            objectFit: 'cover',
                                            width: '100%',
                                            height: '100%',
                                        }}
                                    />
                                </ImageListItem>
                            ))}
                        </ImageList>
                    )}

                    {/* Display documents if present */}
                    {message.documents && message.documents.length > 0 && (
                        <Box sx={{ mb: 2 }}>
                            <Typography variant="caption" sx={{ color: theme.palette.text.secondary, mb: 1, display: 'block' }}>
                                <span aria-hidden="true">📄</span> Attached Documents:
                            </Typography>
                            <List sx={{ padding: 0 }}>
                                {message.documents.map((doc, index) => (
                                    <ListItem
                                        key={doc.id || index}
                                        component="a"
                                        href={doc.url}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        sx={{
                                            bgcolor: theme.palette.action.hover,
                                            borderRadius: 1,
                                            mb: 0.5,
                                            py: 1,
                                            cursor: 'pointer',
                                            color: 'inherit',
                                            textDecoration: 'none',
                                            '&:hover': {
                                                bgcolor: theme.palette.action.selected,
                                            },
                                        }}
                                        secondaryAction={
                                            <OpenInNew
                                                fontSize="small"
                                                aria-hidden="true"
                                                sx={{ color: theme.palette.text.secondary }}
                                            />
                                        }
                                    >
                                        <ListItemIcon sx={{ color: theme.palette.text.secondary, minWidth: 40 }}>
                                            {getDocumentIcon(doc.filename)}
                                        </ListItemIcon>
                                        <ListItemText
                                            primary={doc.filename}
                                            secondary={
                                                <>
                                                    {formatFileSize(doc.size)}
                                                    {doc.pageCount && ` • ${doc.pageCount} page${doc.pageCount > 1 ? 's' : ''}`}
                                                    {doc.wordCount && ` • ${doc.wordCount.toLocaleString()} words`}
                                                </>
                                            }
                                            primaryTypographyProps={{
                                                sx: { color: theme.palette.text.primary, fontSize: '0.9rem' }
                                            }}
                                            secondaryTypographyProps={{
                                                sx: { color: theme.palette.text.secondary, fontSize: '0.75rem' }
                                            }}
                                        />
                                    </ListItem>
                                ))}
                            </List>
                        </Box>
                    )}

                    {/* Display message content as Markdown */}
                    <Box
                        sx={{
                            color: theme.palette.text.primary,
                            fontSize: '0.95rem',
                            lineHeight: 1.7,
                            textAlign: isUser ? 'right' : 'left',
                            '& p': { margin: 0, mb: 1 },
                            '& ul, & ol': { pl: 3, mb: 1 },
                            '& code': {
                                fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace',
                                fontSize: '0.85em',
                                bgcolor: theme.palette.action.hover,
                                px: 0.5,
                                py: 0.25,
                                borderRadius: 0.5,
                            },
                            '& pre': {
                                fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace',
                                fontSize: '0.85em',
                                bgcolor: theme.palette.action.hover,
                                p: 1.5,
                                borderRadius: 1,
                                overflowX: 'auto',
                            },
                        }}
                    >
                        <ReactMarkdown
                            remarkPlugins={[remarkGfm]}
                            components={{
                                a: ({ href, children }) => (
                                    <MuiLink href={href} target="_blank" rel="noopener noreferrer">
                                        {children}
                                    </MuiLink>
                                ),
                            }}
                        >
                            {message.content}
                        </ReactMarkdown>
                    </Box>

                    {/* Document citations (RAG sources) */}
                    {!isUser && message.citations && message.citations.length > 0 && (
                        <Box sx={{ mt: 1.5 }}>
                            <Typography
                                variant="caption"
                                sx={{ color: theme.palette.text.secondary, display: 'block', mb: 0.5 }}
                            >
                                Sources:
                            </Typography>
                            <Stack direction="row" spacing={1} flexWrap="wrap">
                                {message.citations.map((citation, index) => (
                                    <Chip
                                        key={`${citation.documentId}-${index}`}
                                        label={`${citation.documentName} (p. ${citation.page})`}
                                        size="small"
                                        variant="outlined"
                                        clickable
                                        component="a"
                                        href={citation.blobUrl}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        sx={{ cursor: 'pointer' }}
                                    />
                                ))}
                            </Stack>
                        </Box>
                    )}

                    {/* Action buttons for AI messages */}
                    {!isUser && (
                        <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                            <Tooltip title={copied ? "Copied!" : "Copy"}>
                                <IconButton
                                    size="small"
                                    onClick={handleCopy}
                                    sx={{
                                        color: theme.palette.text.secondary,
                                        '&:hover': {
                                            color: theme.palette.text.primary,
                                            bgcolor: theme.palette.action.hover,
                                        },
                                    }}
                                >
                                    <ContentCopy fontSize="small" />
                                </IconButton>
                            </Tooltip>
                            {onRetry && (
                                <Tooltip title="Regenerate">
                                    <IconButton
                                        size="small"
                                        onClick={onRetry}
                                        sx={{
                                            color: theme.palette.text.secondary,
                                            '&:hover': {
                                                color: theme.palette.text.primary,
                                                bgcolor: theme.palette.action.hover,
                                            },
                                        }}
                                    >
                                        <Refresh fontSize="small" />
                                    </IconButton>
                                </Tooltip>
                            )}
                        </Box>
                    )}
                </Box>
            </Box>
        </Box>
    );
}

export default MessageItem;
