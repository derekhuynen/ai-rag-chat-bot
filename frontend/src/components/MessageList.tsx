import { Box } from '@mui/material';
import type { Message } from '../types';
import MessageItem from './MessageItem';
import { useEffect, useRef } from 'react';

interface MessageListProps {
    messages: Message[];
    onRetry?: (messageIndex: number) => void;
}

function MessageList({ messages, onRetry }: MessageListProps) {
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    };

    useEffect(() => {
        scrollToBottom();
    }, [messages]);

    return (
        <Box>
            {messages.map((message, index) => (
                <MessageItem
                    key={message.id}
                    message={message}
                    onRetry={message.role === 'assistant' && onRetry ? () => onRetry(index) : undefined}
                />
            ))}
            <div ref={messagesEndRef} />
        </Box>
    );
}

export default MessageList;
