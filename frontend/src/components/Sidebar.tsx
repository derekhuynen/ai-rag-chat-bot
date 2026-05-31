import {
    Box,
    Drawer,
    List,
    ListItem,
    ListItemButton,
    ListItemIcon,
    ListItemText,
    IconButton,
    Typography,
    Divider,
    Avatar,
    Menu,
    MenuItem,
    CircularProgress,
    Button,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    DialogActions,
    useTheme,
} from '@mui/material';
import {
    Add as AddIcon,
    Chat as ChatIcon,
    Settings as SettingsIcon,
    Logout as LogoutIcon,
    Delete as DeleteIcon,
    Login as LoginIcon,
    MenuOpen as MenuOpenIcon,
    Menu as MenuIcon,
    Brightness4 as DarkModeIcon,
    Brightness7 as LightModeIcon,
    Dashboard as DashboardIcon,
    Article as ArticleIcon,
} from '@mui/icons-material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useConversations, useDeleteConversation } from '../hooks/useConversations';
import { useAuth } from '../hooks/useAuth';
import { useThemeMode } from '../contexts/theme-context';
import { getSidebarColors, getAvatarColors } from '../theme/theme';
import LoginModal from './LoginModal';
import RegisterModal from './RegisterModal';

interface SidebarProps {
    onNewChat: () => void;
    activeConversationId: string | null;
    onSelectConversation: (conversationId: string) => void;
    width: number;
    onWidthChange: (width: number) => void;
    collapsed: boolean;
    onToggleCollapse: () => void;
}

function Sidebar({ onNewChat, activeConversationId, onSelectConversation, width, onWidthChange, collapsed, onToggleCollapse }: SidebarProps) {
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const [loginOpen, setLoginOpen] = useState(false);
    const [registerOpen, setRegisterOpen] = useState(false);
    const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);
    const open = Boolean(anchorEl);
    const navigate = useNavigate();
    const { user, logout } = useAuth();
    const { data: conversations, isLoading } = useConversations(!!user);
    const deleteConversation = useDeleteConversation();
    const theme = useTheme();
    const { mode, toggleTheme } = useThemeMode();
    const sidebarColors = getSidebarColors(mode);

    const handleClick = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleClose = () => {
        setAnchorEl(null);
    };

    const handleLogout = () => {
        logout();
        handleClose();
        onNewChat();
    };

    const handleDelete = (e: React.MouseEvent, conversationId: string) => {
        e.stopPropagation();
        setPendingDeleteId(conversationId);
    };

    const handleCancelDelete = () => {
        setPendingDeleteId(null);
    };

    const handleConfirmDelete = () => {
        if (pendingDeleteId) {
            deleteConversation.mutate(pendingDeleteId);
            if (activeConversationId === pendingDeleteId) {
                onNewChat();
            }
        }
        setPendingDeleteId(null);
    };

    const handleMouseDown = (e: React.MouseEvent) => {
        e.preventDefault();
        const startX = e.clientX;
        const startWidth = width;

        const handleMouseMove = (moveEvent: MouseEvent) => {
            const delta = moveEvent.clientX - startX;
            const newWidth = Math.max(200, Math.min(500, startWidth + delta));
            onWidthChange(newWidth);
        };

        const handleMouseUp = () => {
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
        };

        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', handleMouseUp);
    };

    const handleResizeKeyDown = (e: React.KeyboardEvent) => {
        const step = 16;
        if (e.key === 'ArrowLeft') {
            e.preventDefault();
            onWidthChange(Math.max(200, Math.min(500, width - step)));
        } else if (e.key === 'ArrowRight') {
            e.preventDefault();
            onWidthChange(Math.max(200, Math.min(500, width + step)));
        }
    };

    return (
        <>
            <Drawer
                variant="permanent"
                sx={{
                    width: collapsed ? 50 : width,
                    flexShrink: 0,
                    transition: 'width 0.3s',
                    '& .MuiDrawer-paper': {
                        width: collapsed ? 50 : width,
                        boxSizing: 'border-box',
                        bgcolor: sidebarColors.background,
                        color: theme.palette.text.primary,
                        borderRight: 'none',
                        transition: 'width 0.3s',
                        overflowX: 'hidden',
                    },
                }}
            >
                <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
                    {/* Collapse Button */}
                    <Box sx={{ p: 1, display: 'flex', justifyContent: collapsed ? 'center' : 'space-between', alignItems: 'center' }}>
                        {!collapsed && (
                            <IconButton
                                onClick={toggleTheme}
                                size="small"
                                sx={{
                                    color: sidebarColors.icon,
                                    '&:hover': {
                                        bgcolor: sidebarColors.hover,
                                    },
                                }}
                            >
                                {mode === 'dark' ? <LightModeIcon fontSize="small" /> : <DarkModeIcon fontSize="small" />}
                            </IconButton>
                        )}
                        <IconButton
                            onClick={onToggleCollapse}
                            sx={{
                                color: sidebarColors.icon,
                                '&:hover': {
                                    bgcolor: sidebarColors.hover,
                                },
                            }}
                        >
                            {collapsed ? <MenuIcon /> : <MenuOpenIcon />}
                        </IconButton>
                    </Box>

                    {/* New Chat Button */}
                    {!collapsed && <Box sx={{ p: 2, pt: 0 }}>
                        <ListItemButton
                            onClick={onNewChat}
                            sx={{
                                border: `1px solid ${sidebarColors.border}`,
                                borderRadius: 1,
                                '&:hover': {
                                    bgcolor: sidebarColors.hover,
                                },
                            }}
                        >
                            <ListItemIcon sx={{ minWidth: 36, color: theme.palette.text.primary }}>
                                <AddIcon />
                            </ListItemIcon>
                            <ListItemText primary="New chat" />
                        </ListItemButton>
                    </Box>}

                    {/* New Chat Icon Button (Collapsed) */}
                    {collapsed && <Box sx={{ p: 1, display: 'flex', justifyContent: 'center' }}>
                        <IconButton
                            onClick={onNewChat}
                            sx={{
                                color: sidebarColors.icon,
                                '&:hover': {
                                    bgcolor: sidebarColors.hover,
                                },
                            }}
                        >
                            <AddIcon />
                        </IconButton>
                    </Box>}

                    {!collapsed && <Divider sx={{ borderColor: theme.palette.divider }} />}

                    {/* Spacer for collapsed sidebar to push profile to bottom */}
                    {collapsed && <Box sx={{ flex: 1 }} />}

                    {/* Chat History */}
                    {!collapsed && <Box sx={{ flex: 1, overflowY: 'auto', px: 2, py: 1 }}>
                        {isLoading && (
                            <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                                <CircularProgress size={24} sx={{ color: theme.palette.primary.main }} />
                            </Box>
                        )}

                        {!isLoading && conversations && conversations.length > 0 && (
                            <>
                                <Typography variant="caption" sx={{ px: 2, py: 1, color: theme.palette.text.secondary }}>
                                    Recent
                                </Typography>
                                <List dense>
                                    {conversations.map((conversation) => (
                                        <ListItem
                                            key={conversation.id}
                                            disablePadding
                                            secondaryAction={
                                                <IconButton
                                                    edge="end"
                                                    size="small"
                                                    onClick={(e) => handleDelete(e, conversation.id)}
                                                    sx={{
                                                        color: sidebarColors.icon,
                                                        '&:hover': { color: theme.palette.text.primary },
                                                    }}
                                                >
                                                    <DeleteIcon fontSize="small" />
                                                </IconButton>
                                            }
                                        >
                                            <ListItemButton
                                                selected={activeConversationId === conversation.id}
                                                onClick={() => onSelectConversation(conversation.id)}
                                                sx={{
                                                    borderRadius: 1,
                                                    pr: 6,
                                                    '&:hover': {
                                                        bgcolor: sidebarColors.hover,
                                                    },
                                                    '&.Mui-selected': {
                                                        bgcolor: sidebarColors.selected,
                                                        '&:hover': {
                                                            bgcolor: theme.palette.action.selected,
                                                        },
                                                    },
                                                }}
                                            >
                                                <ListItemIcon sx={{ minWidth: 36, color: theme.palette.text.primary }}>
                                                    <ChatIcon fontSize="small" />
                                                </ListItemIcon>
                                                <ListItemText
                                                    primary={conversation.title}
                                                    primaryTypographyProps={{
                                                        fontSize: '0.875rem',
                                                        noWrap: true,
                                                    }}
                                                />
                                            </ListItemButton>
                                        </ListItem>
                                    ))}
                                </List>
                            </>
                        )}

                        {!isLoading && (!conversations || conversations.length === 0) && (
                            <Typography variant="body2" sx={{ px: 2, py: 3, color: theme.palette.text.secondary, textAlign: 'center' }}>
                                No conversations yet
                            </Typography>
                        )}
                    </Box>}

                    {!collapsed && <Divider sx={{ borderColor: theme.palette.divider }} />}

                    {/* Account Section */}
                    {!collapsed && <Box sx={{ p: 2 }}>
                        {!user ? (
                            <Button
                                fullWidth
                                startIcon={<LoginIcon />}
                                onClick={() => setLoginOpen(true)}
                                sx={{
                                    color: theme.palette.text.primary,
                                    borderColor: sidebarColors.border,
                                    border: '1px solid',
                                    borderRadius: 1,
                                    textTransform: 'none',
                                    '&:hover': {
                                        bgcolor: sidebarColors.hover,
                                        borderColor: theme.palette.divider,
                                    },
                                }}
                            >
                                Log in
                            </Button>
                        ) : (
                            <>
                                <ListItemButton
                                    onClick={handleClick}
                                    sx={{
                                        borderRadius: 1,
                                        '&:hover': {
                                            bgcolor: sidebarColors.hover,
                                        },
                                    }}
                                >
                                    <Avatar sx={{ width: 32, height: 32, mr: 1.5, bgcolor: getAvatarColors('assistant', mode) }}>
                                        {user.name.charAt(0).toUpperCase()}
                                    </Avatar>
                                    <ListItemText
                                        primary={user.name}
                                        primaryTypographyProps={{
                                            fontSize: '0.875rem',
                                        }}
                                    />
                                </ListItemButton>

                                <Menu
                                    anchorEl={anchorEl}
                                    open={open}
                                    onClose={handleClose}
                                    transformOrigin={{ horizontal: 'left', vertical: 'bottom' }}
                                    anchorOrigin={{ horizontal: 'left', vertical: 'top' }}
                                    PaperProps={{
                                        sx: {
                                            bgcolor: theme.palette.background.paper,
                                            color: theme.palette.text.primary,
                                        },
                                    }}
                                >
                                    {user.role === 'Admin' && (
                                        <>
                                            <MenuItem onClick={() => { navigate('/admin'); handleClose(); }}>
                                                <ListItemIcon>
                                                    <DashboardIcon fontSize="small" sx={{ color: theme.palette.text.primary }} />
                                                </ListItemIcon>
                                                Admin Dashboard
                                            </MenuItem>
                                            <MenuItem onClick={() => { navigate('/admin/documents'); handleClose(); }}>
                                                <ListItemIcon>
                                                    <ArticleIcon fontSize="small" sx={{ color: theme.palette.text.primary }} />
                                                </ListItemIcon>
                                                Documents
                                            </MenuItem>
                                        </>
                                    )}
                                    <MenuItem onClick={handleClose}>
                                        <ListItemIcon>
                                            <SettingsIcon fontSize="small" sx={{ color: theme.palette.text.primary }} />
                                        </ListItemIcon>
                                        Settings
                                    </MenuItem>
                                    <MenuItem onClick={handleLogout}>
                                        <ListItemIcon>
                                            <LogoutIcon fontSize="small" sx={{ color: theme.palette.text.primary }} />
                                        </ListItemIcon>
                                        Log out
                                    </MenuItem>
                                </Menu>
                            </>
                        )}
                    </Box>}

                    {/* Profile Icon Button (Collapsed) */}
                    {collapsed && <Box sx={{ p: 1, display: 'flex', justifyContent: 'center' }}>
                        {!user ? (
                            <IconButton
                                onClick={() => setLoginOpen(true)}
                                sx={{
                                    color: sidebarColors.icon,
                                    '&:hover': {
                                        bgcolor: sidebarColors.hover,
                                    },
                                }}
                            >
                                <LoginIcon />
                            </IconButton>
                        ) : (
                            <IconButton
                                onClick={handleClick}
                                sx={{
                                    color: sidebarColors.icon,
                                    '&:hover': {
                                        bgcolor: sidebarColors.hover,
                                    },
                                }}
                            >
                                <Avatar sx={{ width: 32, height: 32, bgcolor: getAvatarColors('assistant', mode) }}>
                                    {user.name.charAt(0).toUpperCase()}
                                </Avatar>
                            </IconButton>
                        )}
                    </Box>}
                </Box>

                <LoginModal
                    open={loginOpen}
                    onClose={() => setLoginOpen(false)}
                    onSwitchToRegister={() => {
                        setLoginOpen(false);
                        setRegisterOpen(true);
                    }}
                />

                <RegisterModal
                    open={registerOpen}
                    onClose={() => setRegisterOpen(false)}
                    onSwitchToLogin={() => {
                        setRegisterOpen(false);
                        setLoginOpen(true);
                    }}
                />
            </Drawer>

            {/* Resize Handle */}
            <Box
                role="separator"
                aria-label="Resize sidebar"
                aria-orientation="vertical"
                aria-valuenow={width}
                aria-valuemin={200}
                aria-valuemax={500}
                tabIndex={0}
                onMouseDown={handleMouseDown}
                onKeyDown={handleResizeKeyDown}
                sx={{
                    position: 'fixed',
                    left: width,
                    top: 0,
                    bottom: 0,
                    width: '4px',
                    cursor: 'col-resize',
                    bgcolor: 'transparent',
                    zIndex: 1300,
                    '&:hover': {
                        bgcolor: theme.palette.action.hover,
                    },
                    '&:focus-visible': {
                        bgcolor: theme.palette.primary.main,
                        outline: 'none',
                    },
                    transition: 'background-color 0.2s',
                }}
            />

            {/* Delete Conversation Confirmation */}
            <Dialog open={pendingDeleteId !== null} onClose={handleCancelDelete}>
                <DialogTitle>Delete conversation?</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        This conversation will be permanently deleted. This action cannot be undone.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCancelDelete}>Cancel</Button>
                    <Button onClick={handleConfirmDelete} color="error" autoFocus>
                        Delete
                    </Button>
                </DialogActions>
            </Dialog>
        </>
    );
}

export default Sidebar;
