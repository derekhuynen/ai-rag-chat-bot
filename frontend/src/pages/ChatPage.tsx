import { Box, Typography } from '@mui/material';
import { useState } from 'react';
import MessageList from '../components/MessageList';
import MessageInput from '../components/MessageInput';
import Sidebar from '../components/Sidebar';
import type { Message, ImageAttachment, DocumentAttachment } from '../types';
import { useChatStream } from '../hooks/useChatStream';
import { useCreateConversation, useConversation } from '../hooks/useConversations';
import { useAuth } from '../hooks/useAuth';
import LoginIcon from '@mui/icons-material/Login';
import { useTheme } from '@mui/material/styles';

function ChatPage() {
    const { user } = useAuth();
    const theme = useTheme();
    const [messages, setMessages] = useState<Message[]>([]);
    const [activeConversationId, setActiveConversationId] = useState<string | null>(null);
    const [shouldLoadConversation, setShouldLoadConversation] = useState(false);
    const [sidebarWidth, setSidebarWidth] = useState(() => {
        const saved = localStorage.getItem('sidebarWidth');
        return saved ? parseInt(saved) : 260;
    });
    const [sidebarCollapsed, setSidebarCollapsed] = useState(() => {
        const saved = localStorage.getItem('sidebarCollapsed');
        return saved === 'true';
    });
    const { isStreaming, streamingMessage, sendStreamingMessage } = useChatStream();
    const createConversation = useCreateConversation();
    const { data: activeConversation } = useConversation(shouldLoadConversation ? activeConversationId : null);

    const handleSidebarWidthChange = (width: number) => {
        setSidebarWidth(width);
        localStorage.setItem('sidebarWidth', width.toString());
    };

    const handleToggleSidebar = () => {
        setSidebarCollapsed(!sidebarCollapsed);
        localStorage.setItem('sidebarCollapsed', (!sidebarCollapsed).toString());
    };

    // Copy the selected conversation's messages into editable local state once it
    // loads. Done during render (guarded by shouldLoadConversation, which is cleared
    // immediately) rather than in an effect, per React's "you might not need an
    // effect" guidance, which avoids the cascading-render lint warning.
    if (activeConversation && shouldLoadConversation) {
        setMessages(activeConversation.messages.map((msg) => ({
            ...msg,
            timestamp: new Date(msg.timestamp || new Date()),
        })));
        setShouldLoadConversation(false);
    }

    const handleNewChat = async () => {
        setMessages([]);
        setActiveConversationId(null);
        setShouldLoadConversation(false);
    };

    const handleSelectConversation = (conversationId: string) => {
        setActiveConversationId(conversationId);
        setShouldLoadConversation(true);
    };

    const handleSendMessage = async (content: string, images?: ImageAttachment[], documents?: DocumentAttachment[]) => {
        // Create conversation if this is the first message
        let conversationId = activeConversationId;
        if (!conversationId && messages.length === 0) {
            const title = content.substring(0, 50) + (content.length > 50 ? '...' : '');
            const newConversation = await createConversation.mutateAsync({ title });
            conversationId = newConversation.id;
            setActiveConversationId(conversationId);
        }

        // Add user message
        const userMessage: Message = {
            id: Date.now().toString(),
            role: 'user',
            content,
            timestamp: new Date(),
            images,
            documents,
        };
        setMessages((prev) => [...prev, userMessage]);

        // Send to AI and stream response with conversationId, images, and documents
        await sendStreamingMessage(
            content,
            messages,
            (aiMessage) => {
                setMessages((prev) => [...prev, aiMessage]);
            },
            conversationId,
            images,
            documents
        );
    };

    const handleRetry = (messageIndex: number) => {
        // Find the user message that triggered this AI response
        if (messageIndex > 0 && messages[messageIndex - 1]?.role === 'user') {
            const userMessage = messages[messageIndex - 1];

            // Remove the AI message and any messages after it
            setMessages((prev) => prev.slice(0, messageIndex));

            // Resend the user message
            setTimeout(() => {
                handleSendMessage(userMessage.content, userMessage.images, userMessage.documents);
            }, 100);
        }
    };

    // Combine messages with streaming message for display
    const displayMessages =
        isStreaming && streamingMessage
            ? [
                ...messages,
                {
                    id: 'streaming',
                    role: 'assistant' as const,
                    content: streamingMessage,
                    timestamp: new Date(),
                },
            ]
            : messages;

    return (
        <Box sx={{ display: 'flex', height: '100vh', width: '100vw', bgcolor: theme.palette.background.default, overflow: 'hidden' }}>
            <Sidebar
                onNewChat={handleNewChat}
                activeConversationId={activeConversationId}
                onSelectConversation={handleSelectConversation}
                width={sidebarWidth}
                onWidthChange={handleSidebarWidthChange}
                collapsed={sidebarCollapsed}
                onToggleCollapse={handleToggleSidebar}
            />

            <Box
                component="main"
                sx={{
                    flex: 1,
                    display: 'flex',
                    flexDirection: 'column',
                    height: '100vh',
                    overflow: 'hidden',
                    minWidth: 0,
                }}
            >
                {/* Show login prompt if not authenticated */}
                {!user && (
                    <Box
                        sx={{
                            flex: 1,
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            flexDirection: 'column',
                            gap: 3,
                        }}
                    >
                        <LoginIcon sx={{ fontSize: 80, color: theme.palette.text.disabled }} />
                        <Typography variant="h4" sx={{ color: theme.palette.text.primary, fontWeight: 500 }}>
                            Welcome to AI ChatBot
                        </Typography>
                        <Typography variant="body1" sx={{ color: theme.palette.text.secondary, textAlign: 'center', maxWidth: '400px' }}>
                            Please log in or create an account to start chatting with AI
                        </Typography>
                        <Typography variant="body2" sx={{ color: theme.palette.text.disabled }}>
                            Click the "Log in" button in the bottom left to get started
                        </Typography>
                    </Box>
                )}

                {/* Header - only show when no messages and authenticated */}
                {user && messages.length === 0 && (
                    <Box
                        sx={{
                            flex: 1,
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            flexDirection: 'column',
                            gap: 2,
                        }}
                    >
                        <Typography variant="h4" sx={{ color: theme.palette.text.primary, fontWeight: 500 }}>
                            AI ChatBot
                        </Typography>
                        <Typography variant="body1" sx={{ color: theme.palette.text.secondary }}>
                            How can I help you today?
                        </Typography>
                    </Box>
                )}

                {/* Messages */}
                {messages.length > 0 && (
                    <Box sx={{ flex: 1, overflowY: 'auto' }}>
                        <MessageList messages={displayMessages} onRetry={handleRetry} />
                    </Box>
                )}

                {/* Input - only show when authenticated */}
                {user && (
                    <Box sx={{ borderTop: `1px solid ${theme.palette.divider}`, pb: 3 }}>
                        <Box sx={{ maxWidth: '64rem', mx: 'auto', px: 3, pt: 3 }}>
                            <MessageInput
                                onSendMessage={handleSendMessage}
                                disabled={isStreaming}
                                conversationId={activeConversationId || undefined}
                            />
                        </Box>
                    </Box>
                )}
            </Box>
        </Box>
    );
}

export default ChatPage;
