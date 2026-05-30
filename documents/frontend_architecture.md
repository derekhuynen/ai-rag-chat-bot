# Frontend Architecture Documentation

## Overview

React-based SPA using TypeScript, Vite, Material UI, and React Query for efficient state management and data fetching.

## Technology Stack

- **Framework**: React 19 with TypeScript
- **Build Tool**: Vite 7
- **UI Library**: Material UI (MUI) 7
- **State Management**: React Query (TanStack Query) for server state
- **Form Management**: react-hook-form with Zod validation
- **HTTP Client**: Axios for REST calls, native fetch for streaming (SSE)
- **Routing**: React Router v7

## Project Structure

```
frontend/src/
├── components/           # Reusable UI components
│   ├── LoginModal.tsx
│   ├── RegisterModal.tsx
│   ├── MessageInput.tsx
│   ├── MessageList.tsx
│   ├── MessageItem.tsx   # Markdown + citations + attachments
│   └── Sidebar.tsx
├── pages/               # Page-level components
│   ├── ChatPage.tsx
│   ├── AdminDashboard.tsx
│   └── AdminDocumentsPage.tsx
├── contexts/            # React contexts
│   ├── AuthContext.tsx
│   └── ThemeContext.tsx
├── services/            # API and business logic
│   ├── api.ts           # Axios instance with interceptors
│   ├── authService.ts   # Authentication API calls
│   ├── chatService.ts   # Chat and streaming API calls
│   ├── imageUploadService.ts
│   └── documentService.ts
├── hooks/               # Custom React hooks
│   ├── useAuth.ts       # Authentication context and hooks
│   ├── useChatStream.ts # Streaming chat (SSE)
│   └── useConversations.ts
├── theme/               # Theme helpers
│   └── theme.ts         # Color constants and style objects
├── utils/               # Utility functions and constants
│   ├── storage.ts       # Centralized token storage
│   ├── errors.ts        # Error handling utilities
│   └── index.ts
├── types/               # TypeScript type definitions
│   └── index.ts
└── App.tsx              # Root component (routes + providers)
```

## Key Architectural Patterns

### 1. Service Layer Pattern

All API calls are centralized in the `services/` folder:

- **api.ts**: Axios instance with request/response interceptors for authentication
- **authService.ts**: Login, register, logout, token management
- **chatService.ts**: Chat messages, streaming responses, conversation management

**Benefits**:

- Single source of truth for API calls
- Consistent error handling
- Easy to mock for testing
- Centralized authentication token injection

### 2. Custom Hooks for State Management

React Query hooks in `hooks/` provide:

- Server state caching and synchronization
- Automatic refetching and background updates
- Optimistic updates for better UX
- Loading and error states

**Example**: `useConversations.ts`

```typescript
export const useConversations = () => {
	return useQuery({
		queryKey: ['conversations'],
		queryFn: conversationService.getUserConversations,
		staleTime: 1000 * 60 * 5, // 5 minutes
	});
};
```

### 3. Centralized Utilities

#### Storage Utility (`utils/storage.ts`)

- Single `TOKEN_KEY` constant
- Handles both localStorage (persistent) and sessionStorage (session-only)
- Methods: `getToken()`, `setToken()`, `removeToken()`, `clearAll()`
- Eliminates duplicate token retrieval logic across multiple files

#### Theme Utility (`utils/theme.ts`)

- Centralized color palette
- Reusable style objects for Material-UI components
- Constants: `colors`, `darkTextFieldStyles`, `darkDialogStyles`, `darkButtonStyles`, `darkCheckboxStyles`
- Ensures consistent styling across the app

#### Error Utility (`utils/errors.ts`)

- `getErrorMessage()`: Extracts user-friendly error messages from Axios errors, strings, or objects
- `handleAsyncError()`: Promise-based error handling wrapper
- Standardizes error handling patterns

### 4. Authentication Flow

**Token Storage Strategy**:

- User selects "Remember me" → token stored in localStorage (persistent)
- No "Remember me" → token stored in sessionStorage (session-only)
- `storageService.getToken()` checks both storages automatically

**Auto-Login**:

1. App mounts → checks for token via `storageService.getToken()`
2. If token found → validates with backend `/auth/me`
3. If valid → sets user in AuthContext
4. If invalid → removes token and shows login

**Request Authentication**:

- Axios interceptor in `api.ts` adds `Authorization: Bearer {token}` header
- Chat streaming uses fetch with manual header: `storageService.getToken()`

### 5. Form Validation

- **react-hook-form**: Performant form state management
- **Zod**: Runtime schema validation
- **@hookform/resolvers/zod**: Seamless integration

**Example**:

```typescript
const loginSchema = z.object({
	email: z.string().email('Invalid email address'),
	password: z.string().min(6, 'Password must be at least 6 characters'),
});

const {
	register,
	handleSubmit,
	formState: { errors },
} = useForm<LoginFormData>({
	resolver: zodResolver(loginSchema),
});
```

### 6. Streaming Chat Implementation

**Technology**: Server-Sent Events (SSE) via native `fetch` API

**Flow**:

1. User sends message → added to UI immediately
2. `chatService.streamMessage()` opens SSE connection
3. Backend streams token-by-token responses
4. Frontend updates UI in real-time with `onChunk` callback
5. Message saved to database on completion

**Key Feature**: Streaming message state managed separately from message history to prevent flickering

## Component Patterns

### Modal Components

- **LoginModal** and **RegisterModal**: Controlled components with open/close props
- Use `darkDialogStyles`, `darkTextFieldStyles`, `darkButtonStyles` from theme
- Consistent error handling with `getErrorMessage()`
- Form validation with react-hook-form + Zod

### Message Components

- **MessageList**: Container for chat messages (streaming + history)
- **MessageItem**: Renders Markdown (via `react-markdown` + `remark-gfm`), attachments (images/documents), and RAG citation chips
- **MessageInput**: Multi-line input with Enter-to-send (Shift+Enter for newline), image/document attach hooks

### Layout & Admin Components

- **Sidebar**: Conversation history, new chat button, user menu
- **ChatPage**: Main container orchestrating all chat components
- **AdminDashboard**: High-level admin overview
- **AdminDocumentsPage**: Admin-only document upload/list/delete UI for RAG corpus

## State Management Strategy

### Local State

- Component-specific UI state (modals, inputs, toggles)
- Managed with `useState`

### Server State

- React Query for API data (conversations, messages, user data)
- Automatic caching, refetching, and synchronization

### Global State

- **AuthContext**: Current user, login/logout functions
- **ThemeContext**: Light/dark theme toggle and persistence
- Minimal additional global state - prefer composition and prop drilling

## Styling Approach

### Material-UI Theme Customization

- Dark theme enforced via `colors` object in `theme/theme.ts`
- `ThemeProvider` + `ThemeContext` wrap the app in `App.tsx`
- Consistent color palette across all components via `sx` prop and shared helpers

### Color Palette

```typescript
colors: {
  background: { primary, secondary, sidebar, darker, medium, light },
  text: { primary, secondary, muted, disabled },
  border: { default, light, medium, hover },
  button: { primary, primaryHover, secondary, success, successHover, danger, disabled },
  message: { user, assistant },
}
```

### Style Objects

- `darkTextFieldStyles`: Reusable TextField styling (saves ~7 lines per field)
- `darkDialogStyles`: Modal title and content styling
- `darkButtonStyles`: Primary button styling
- `darkCheckboxStyles`: Checkbox checked state styling

## API Integration

### Axios Configuration

- Base URL configured for backend API
- Request interceptor adds `Authorization` header
- Response interceptor handles 401 (auto-logout) and other errors

### Streaming with Fetch

- Axios doesn't support streaming responses well
- Use native `fetch` for SSE connections
- Manual token injection in headers

### Error Handling

1. Service layer catches errors
2. React Query handles query/mutation errors
3. Components display errors via `getErrorMessage()` utility
4. Consistent error format from backend

## Performance Considerations

### Current Optimizations

- React Query caching reduces redundant API calls
- Debouncing on input fields (where applicable)
- Lazy loading for heavy components (planned)

### Future Optimizations

- Virtual scrolling for long message lists
- Code splitting with React.lazy()
- Service worker for offline support
- Optimistic updates for chat messages

## Security Practices

1. **JWT Token Security**:

   - Stored in localStorage/sessionStorage (XSS risk acknowledged)
   - HttpOnly cookies considered for future
   - Token expiration enforced by backend (24 hours)

2. **Input Sanitization**:

   - Zod validation on all forms
   - Markdown rendering sanitizes HTML

3. **CORS**:
   - Backend enforces CORS policy
   - Frontend sends credentials with requests

## Testing Strategy (Planned)

1. **Unit Tests**: Vitest for utilities and hooks
2. **Component Tests**: React Testing Library
3. **E2E Tests**: Playwright
4. **API Mocking**: MSW (Mock Service Worker)

## Build and Deployment

### Development

```bash
npm run dev  # Starts Vite dev server on port 5173
```

### Production Build

```bash
npm run build  # Outputs to dist/
npm run preview  # Preview production build
```

### Environment Variables

- `.env.development`: Local backend URL
- `.env.production`: Production backend URL

## Refactoring Achievements

### Code Duplication Eliminated

1. **Token Retrieval**: Reduced from 6 instances across 3 files to 1 utility
2. **TextField Styling**: Removed ~21 lines per TextField (2 in LoginModal, 3 in RegisterModal)
3. **Error Handling**: Standardized with `getErrorMessage()` utility
4. **Dialog Styling**: Centralized in `darkDialogStyles`

### Before vs After

- **Before**: 6 separate token retrieval implementations
- **After**: 1 centralized `storageService` utility

- **Before**: ~50 lines of duplicate TextField styling across modals
- **After**: 1 `darkTextFieldStyles` constant imported where needed

### Files Refactored

- ✅ `services/api.ts` - Uses `storageService.getToken()`
- ✅ `services/authService.ts` - Delegates to `storageService`
- ✅ `services/chatService.ts` - Uses `storageService.getToken()`
- ✅ `components/LoginModal.tsx` - Uses theme constants, `getErrorMessage()`
- ✅ `components/RegisterModal.tsx` - Uses theme constants, `getErrorMessage()`

## Next Steps

1. Create frontend instruction document for AI guidance
2. Add virtual scrolling to MessageList
3. Implement optimistic updates for chat messages
4. Add loading skeletons for better UX
5. Implement user settings page
6. Add admin dashboard UI
7. Protected route guards
