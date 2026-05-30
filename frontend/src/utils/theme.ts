// Theme constants for consistent styling across the app

export const colors = {
  background: {
    primary: '#343541',
    secondary: '#444654',
    sidebar: '#202123',
    darker: '#171717',
    medium: '#40414f',
    light: '#2d2d2d',
  },
  text: {
    primary: 'white',
    secondary: 'rgba(255, 255, 255, 0.7)',
    muted: 'rgba(255, 255, 255, 0.5)',
    disabled: 'rgba(255, 255, 255, 0.3)',
  },
  border: {
    default: 'rgba(255, 255, 255, 0.1)',
    light: 'rgba(255, 255, 255, 0.2)',
    medium: 'rgba(255, 255, 255, 0.3)',
    hover: 'rgba(255, 255, 255, 0.5)',
  },
  button: {
    primary: '#10a37f',
    primaryHover: '#0d8c6d',
    secondary: '#40414f',
    success: '#19c37d',
    successHover: '#15a869',
    danger: '#ef4444',
    disabled: 'rgba(255, 255, 255, 0.1)',
  },
  message: {
    user: '#343541',
    assistant: '#444654',
  },
};

// Common Material-UI TextField styles for dark theme
export const darkTextFieldStyles = {
  '& .MuiOutlinedInput-root': {
    color: colors.text.primary,
    '& fieldset': { borderColor: colors.border.medium },
    '&:hover fieldset': { borderColor: colors.border.light },
    '&.Mui-focused fieldset': { borderColor: colors.text.secondary },
  },
  '& .MuiInputLabel-root': { color: colors.text.secondary },
  '& .MuiFormHelperText-root': { color: colors.text.muted },
};

// Common dialog styles
export const darkDialogStyles = {
  title: {
    bgcolor: colors.background.primary,
    color: colors.text.primary,
  },
  content: {
    bgcolor: colors.background.primary,
    pt: 2,
  },
};

// Common button styles
export const darkButtonStyles = {
  primary: {
    bgcolor: colors.button.primary,
    '&:hover': { bgcolor: colors.button.primaryHover },
  },
};

// Common checkbox styles
export const darkCheckboxStyles = {
  '&.Mui-checked': { color: colors.button.primary },
};
