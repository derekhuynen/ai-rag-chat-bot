---
applyTo: '/frontend/**'
---

# Frontend Development Rules and Guidelines

## Core Principles

1. **DRY (Don't Repeat Yourself)**: Use utilities, constants, and reusable components
2. **Type Safety**: Leverage TypeScript for all components, hooks, and services
3. **Consistent Patterns**: Follow established patterns for similar functionality
4. **Separation of Concerns**: Keep UI, logic, and API calls separate

## Project Structure Rules

### File Organization

- **Components** (`components/`): Reusable UI components only
- **Pages** (`pages/`): Route-level components that compose smaller components
- **Services** (`services/`): All API calls and external integrations
- **Hooks** (`hooks/`): Custom React hooks for shared logic
- **Utils** (`utils/`): Pure utility functions and constants
- **Types** (`types/`): Shared TypeScript interfaces and types

### Naming Conventions

- **Components**: PascalCase (e.g., `MessageInput.tsx`, `LoginModal.tsx`)
- **Hooks**: camelCase starting with "use" (e.g., `useAuth.ts`, `useChatStream.ts`)
- **Services**: camelCase ending with "Service" (e.g., `authService.ts`, `chatService.ts`)
- **Utils**: camelCase (e.g., `storage.ts`, `theme.ts`, `errors.ts`)
- **Types**: PascalCase for interfaces/types (e.g., `User`, `Message`, `Conversation`)

## Service Layer Pattern

### All API Calls Must Go Through Services

**❌ WRONG** - API call in component:

```typescript
const ChatPage = () => {
	const handleLogin = async () => {
		const response = await axios.post('/api/auth/login', data);
		// ...
	};
};
```

**✅ CORRECT** - API call in service:

```typescript
// services/authService.ts
export const authService = {
	login: async (email: string, password: string) => {
		const response = await api.post('/auth/login', { email, password });
		return response.data;
	},
};

// components/LoginModal.tsx
const handleLogin = async () => {
	const user = await authService.login(email, password);
};
```

### Service Method Patterns

- Return only the data, not the full Axios response
- Let React Query handle loading/error states
- Use TypeScript for request/response types

## Token Storage Rules

### ALWAYS Use storageService

**❌ WRONG**:

```typescript
const token =
	localStorage.getItem('auth_token') || sessionStorage.getItem('auth_token');
localStorage.setItem('auth_token', token);
```

**✅ CORRECT**:

```typescript
import { storageService } from '../utils/storage';

const token = storageService.getToken();
storageService.setToken(token, rememberMe);
```

### Token Storage Guidelines

- Never access localStorage/sessionStorage directly
- Use `storageService.getToken()` - handles both storage types
- Use `storageService.setToken(token, rememberMe)` - handles persistence logic
- Use `storageService.removeToken()` for logout
- Token key is centralized in `TOKEN_KEY` constant

## Styling Rules

### Use Theme Constants, Not Inline Colors

**❌ WRONG**:

```typescript
<TextField
  sx={{
    '& .MuiOutlinedInput-root': {
      color: 'white',
      '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.3)' },
    },
  }}
/>
<Button sx={{ bgcolor: '#10a37f', '&:hover': { bgcolor: '#0d8c6d' } }}>
  Submit
</Button>
```

**✅ CORRECT**:

```typescript
import { darkTextFieldStyles, darkButtonStyles, colors } from '../utils/theme';

<TextField sx={darkTextFieldStyles} />
<Button sx={darkButtonStyles.primary}>Submit</Button>
<Typography sx={{ color: colors.text.secondary }}>Text</Typography>
```

### Available Theme Constants

- `colors.*` - Color palette organized by category
- `darkTextFieldStyles` - TextField styling for dark theme
- `darkDialogStyles.title`, `darkDialogStyles.content` - Dialog styling
- `darkButtonStyles.primary` - Primary button styling
- `darkCheckboxStyles` - Checkbox checked state styling

### When to Extract Styles

- **Do extract** if used in 2+ places
- **Do extract** all color values (use `colors.*` object)
- **Don't extract** one-off layout styles (flexbox, grid, spacing)

## Error Handling Rules

### Use getErrorMessage() Utility

**❌ WRONG**:

```typescript
try {
	await someApiCall();
} catch (err) {
	setError((err as any).response?.data?.error || 'Something went wrong');
}
```

**✅ CORRECT**:

```typescript
import { getErrorMessage } from '../utils/errors';

try {
	await someApiCall();
} catch (err) {
	setError(getErrorMessage(err));
}
```

### Error Handling Best Practices

- Always use `getErrorMessage()` for user-facing error messages
- Don't use `(err as any)` - let the utility handle type safety
- Display errors in UI (Alert, Snackbar, or inline)
- Log unexpected errors to console in development

## Form Validation Rules

### Use react-hook-form + Zod

**Standard Form Pattern**:

```typescript
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const formSchema = z.object({
	email: z.string().email('Invalid email address'),
	password: z.string().min(6, 'Password must be at least 6 characters'),
});

type FormData = z.infer<typeof formSchema>;

const MyForm = () => {
	const {
		register,
		handleSubmit,
		formState: { errors },
	} = useForm<FormData>({
		resolver: zodResolver(formSchema),
	});

	const onSubmit = async (data: FormData) => {
		// Handle submission
	};

	return (
		<form onSubmit={handleSubmit(onSubmit)}>
			<TextField
				{...register('email')}
				error={!!errors.email}
				helperText={errors.email?.message}
			/>
		</form>
	);
};
```

### Validation Guidelines

- Define schemas outside component for reusability
- Use `z.infer<typeof schema>` for type inference
- Display validation errors via `helperText`
- Validation runs on blur and submit by default

## React Query Patterns

### Query Hooks

```typescript
// hooks/useConversations.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

export const useConversations = () => {
	return useQuery({
		queryKey: ['conversations'],
		queryFn: conversationService.getUserConversations,
		staleTime: 1000 * 60 * 5, // 5 minutes
	});
};

export const useCreateConversation = () => {
	const queryClient = useQueryClient();

	return useMutation({
		mutationFn: conversationService.createConversation,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['conversations'] });
		},
	});
};
```

### Query Guidelines

- Use `useQuery` for GET requests
- Use `useMutation` for POST/PUT/DELETE requests
- Invalidate queries after mutations to refetch data
- Set appropriate `staleTime` to reduce unnecessary requests
- Use query keys consistently across the app

## Component Patterns

### Modal Components

```typescript
interface ModalProps {
	open: boolean;
	onClose: () => void;
	// Other props
}

const MyModal = ({ open, onClose }: ModalProps) => {
	return (
		<Dialog open={open} onClose={onClose}>
			<DialogTitle sx={darkDialogStyles.title}>Title</DialogTitle>
			<DialogContent sx={darkDialogStyles.content}>
				{/* Content */}
			</DialogContent>
		</Dialog>
	);
};
```

### Modal Guidelines

- Controlled components (open/onClose props)
- Use `darkDialogStyles` for consistent styling
- Reset form state on close
- Handle loading states with disabled buttons

### Input Components

```typescript
<TextField
	{...register('fieldName')}
	fullWidth
	label='Label'
	error={!!errors.fieldName}
	helperText={errors.fieldName?.message}
	sx={darkTextFieldStyles}
/>
```

### Input Guidelines

- Always use `darkTextFieldStyles` for Material-UI TextFields
- Connect to react-hook-form with `{...register()}`
- Display validation errors with `error` and `helperText`
- Use appropriate input types (email, password, number, etc.)

## TypeScript Rules

### Type Definitions

```typescript
// types/index.ts
export interface User {
	id: string;
	name: string;
	email: string;
	role: 'user' | 'admin';
}

export interface Message {
	id: string;
	role: 'user' | 'assistant';
	content: string;
	timestamp: Date;
}
```

### TypeScript Guidelines

- **Never use `any`** - use `unknown` and type guard if needed
- Export all shared interfaces from `types/index.ts`
- Use union types for enums (`'user' | 'admin'`)
- Infer types from Zod schemas where possible: `z.infer<typeof schema>`
- Use `Partial<T>` for optional fields in updates

## State Management Rules

### Local State (useState)

- Component-specific UI state (modals, inputs, toggles)
- Derived state should be computed, not stored
- Lift state up only when needed by multiple siblings

### Server State (React Query)

- **All API data** should use React Query
- Don't store API responses in `useState`
- Use query keys consistently: `['resourceType', id]`
- Invalidate queries after mutations

### Global State (Context)

- Use sparingly - only for true global state (auth, theme)
- Prefer composition and prop drilling for most cases
- Split contexts by concern (AuthContext, ThemeContext)

## Authentication Patterns

### Protected Components

```typescript
const MyComponent = () => {
	const { user, isLoading } = useAuth();

	if (isLoading) return <CircularProgress />;
	if (!user) return <Navigate to='/login' />;

	return <div>Protected content</div>;
};
```

### Authentication Guidelines

- Check `user` from `useAuth()` for protected content
- Show loading state while checking authentication
- Redirect to login if not authenticated
- Auto-logout on 401 responses (handled by Axios interceptor)

## Performance Best Practices

### Avoid Unnecessary Re-renders

- Use `React.memo()` for expensive components
- Use `useCallback()` for function props
- Use `useMemo()` for expensive computations

### Code Splitting (Future)

```typescript
const HeavyComponent = React.lazy(() => import('./HeavyComponent'));

<Suspense fallback={<CircularProgress />}>
	<HeavyComponent />
</Suspense>;
```

### Performance Guidelines

- Debounce search inputs (future implementation)
- Virtual scrolling for long lists (future implementation)
- Lazy load routes and heavy components
- Optimize images with proper formats and sizes

## Import Organization

### Standard Import Order

```typescript
// 1. React and external libraries
import { useState, useEffect } from 'react';
import { Box, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';

// 2. Internal imports (hooks, services, utils)
import { useAuth } from '../hooks/useAuth';
import { authService } from '../services/authService';
import { getErrorMessage } from '../utils/errors';
import { colors, darkTextFieldStyles } from '../utils/theme';

// 3. Types
import type { User, Message } from '../types';

// 4. Local components (if any)
import MessageItem from './MessageItem';
```

## Testing Guidelines (Future)

### Unit Tests

- Test utilities in `utils/` folder
- Test custom hooks with `@testing-library/react-hooks`
- Mock API calls with MSW

### Component Tests

- Test user interactions, not implementation details
- Mock external dependencies (React Query, services)
- Use `screen.getByRole()` over `getByTestId()`

### Test Organization

- Co-locate tests: `MyComponent.test.tsx` next to `MyComponent.tsx`
- Use descriptive test names: `it('should display error when login fails')`

## Code Review Checklist

Before committing, verify:

- [ ] No inline colors - using `colors.*` from theme
- [ ] No duplicate TextField styles - using `darkTextFieldStyles`
- [ ] No direct localStorage access - using `storageService`
- [ ] No manual error extraction - using `getErrorMessage()`
- [ ] No API calls in components - using services
- [ ] TypeScript errors resolved (no `any` types)
- [ ] React Query used for API data (not `useState`)
- [ ] Forms use react-hook-form + Zod validation
- [ ] Imports organized by category
- [ ] No console.log statements in production code

## Common Mistakes to Avoid

### ❌ Don't Store API Data in useState

```typescript
// WRONG
const [users, setUsers] = useState([]);
useEffect(() => {
	api.get('/users').then(res => setUsers(res.data));
}, []);
```

### ✅ Use React Query Instead

```typescript
// CORRECT
const { data: users, isLoading } = useQuery({
	queryKey: ['users'],
	queryFn: userService.getUsers,
});
```

### ❌ Don't Manually Handle Token in Every Request

```typescript
// WRONG
const token = localStorage.getItem('auth_token');
await axios.post('/api/chat', data, {
	headers: { Authorization: `Bearer ${token}` },
});
```

### ✅ Use Configured Axios Instance

```typescript
// CORRECT - token added automatically by interceptor
import { api } from '../services/api';
await api.post('/chat', data);
```

### ❌ Don't Repeat Styling Code

```typescript
// WRONG
<TextField sx={{ '& .MuiOutlinedInput-root': { color: 'white' } }} />
<TextField sx={{ '& .MuiOutlinedInput-root': { color: 'white' } }} />
```

### ✅ Use Theme Constants

```typescript
// CORRECT
<TextField sx={darkTextFieldStyles} />
<TextField sx={darkTextFieldStyles} />
```

## When to Create New Utilities

Create a utility when:

1. Code is duplicated in 2+ places
2. Logic is complex and benefits from unit testing
3. Functionality is app-wide (not component-specific)

Don't create utilities for:

1. One-off component logic
2. Trivial operations (e.g., single-line functions)
3. Highly specific business logic tied to one feature

## Summary

**Key Takeaways**:

1. Services for API calls, not components
2. storageService for tokens, not localStorage
3. Theme constants for styling, not inline colors
4. getErrorMessage() for errors, not manual extraction
5. React Query for server state, not useState
6. react-hook-form + Zod for forms, not manual validation

**Before adding new code, ask**:

- Am I duplicating logic that already exists?
- Should this be a reusable utility/constant?
- Am I following established patterns in the codebase?
- Does this break any of the rules above?

By following these guidelines, the frontend will remain maintainable, consistent, and easy to understand.
