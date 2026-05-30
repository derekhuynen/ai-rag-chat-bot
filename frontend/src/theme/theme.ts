import { createTheme, type ThemeOptions } from '@mui/material/styles';

export const getTheme = (mode: 'light' | 'dark') => {
  const isDark = mode === 'dark';

  const themeOptions: ThemeOptions = {
    palette: {
      mode,
      primary: {
        main: isDark ? '#19c37d' : '#10a37f',
        light: isDark ? '#2dd48f' : '#19c37d',
        dark: isDark ? '#15a869' : '#0d8c68',
        contrastText: '#ffffff',
      },
      secondary: {
        main: isDark ? '#5436da' : '#6b46e5',
        light: isDark ? '#7050e0' : '#8b6ae8',
        dark: isDark ? '#3d27b0' : '#4e2fb8',
        contrastText: '#ffffff',
      },
      background: {
        default: isDark ? '#343541' : '#ffffff',
        paper: isDark ? '#444654' : '#f9fafb',
      },
      text: {
        primary: isDark ? '#ececf1' : '#2d3748',
        secondary: isDark ? 'rgba(236, 236, 241, 0.6)' : 'rgba(45, 55, 72, 0.7)',
      },
      divider: isDark ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.08)',
      action: {
        hover: isDark ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.04)',
        selected: isDark ? 'rgba(255, 255, 255, 0.15)' : 'rgba(0, 0, 0, 0.06)',
      },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            scrollbarColor: isDark ? '#565869 #2d2d3d' : '#d4d4d8 #f4f4f5',
            '&::-webkit-scrollbar, & *::-webkit-scrollbar': {
              width: '8px',
              height: '8px',
            },
            '&::-webkit-scrollbar-thumb, & *::-webkit-scrollbar-thumb': {
              borderRadius: '8px',
              backgroundColor: isDark ? '#565869' : '#d4d4d8',
            },
            '&::-webkit-scrollbar-track, & *::-webkit-scrollbar-track': {
              backgroundColor: isDark ? '#2d2d3d' : '#f4f4f5',
            },
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            textTransform: 'none',
            borderRadius: '8px',
          },
        },
      },
      MuiIconButton: {
        styleOverrides: {
          root: {
            borderRadius: '8px',
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
          },
        },
      },
    },
    shape: {
      borderRadius: 8,
    },
    typography: {
      fontFamily: '"Söhne", "ui-sans-serif", "system-ui", "-apple-system", "Segoe UI", sans-serif',
    },
  };

  return createTheme(themeOptions);
};

// Sidebar-specific colors
export const getSidebarColors = (mode: 'light' | 'dark') => ({
  background: mode === 'dark' ? '#171717' : '#f3f4f6',
  hover: mode === 'dark' ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.05)',
  selected: mode === 'dark' ? 'rgba(255, 255, 255, 0.15)' : 'rgba(0, 0, 0, 0.08)',
  border: mode === 'dark' ? 'rgba(255, 255, 255, 0.2)' : 'rgba(0, 0, 0, 0.15)',
  icon: mode === 'dark' ? 'rgba(255, 255, 255, 0.7)' : 'rgba(0, 0, 0, 0.65)',
});

// User avatar colors
export const getAvatarColors = (role: 'user' | 'assistant', mode: 'light' | 'dark') => {
  if (role === 'user') {
    return mode === 'dark' ? '#5436da' : '#6b46e5';
  }
  return mode === 'dark' ? '#10a37f' : '#19c37d';
};
