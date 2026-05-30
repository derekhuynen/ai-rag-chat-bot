# Frontend Architecture

## Overview

A React single-page app in TypeScript, built with Vite and Material UI, using TanStack Query for server state and native `fetch` for SSE streaming.

## Technology stack

- **Framework:** React 19 + TypeScript
- **Build:** Vite 7
- **UI:** Material UI (MUI) 7, dark theme
- **Server state:** TanStack Query
- **Forms:** react-hook-form + Zod
- **HTTP:** Axios for REST, native `fetch` for streaming (SSE)
- **Routing:** React Router v7

## Project structure

```
frontend/src/
├── components/                 # Reusable UI
│   ├── LoginModal.tsx
│   ├── RegisterModal.tsx
│   ├── MessageInput.tsx
│   ├── MessageList.tsx
│   ├── MessageItem.tsx         # Markdown + citations + attachments
│   ├── Sidebar.tsx
│   └── ProtectedRoute.tsx      # Auth/admin route guard
├── pages/
│   ├── ChatPage.tsx
│   ├── AdminDashboard.tsx
│   └── AdminDocumentsPage.tsx
├── contexts/                   # AuthContext, ThemeContext
├── services/                   # API layer
│   ├── api.ts                  # Axios instance + interceptors
│   ├── authService.ts
│   ├── chatService.ts          # chat + SSE streaming
│   ├── conversationService.ts
│   ├── adminService.ts
│   ├── documentManagementService.ts  # admin RAG documents
│   ├── documentUploadService.ts      # in-chat document attachments
│   └── imageUploadService.ts
├── hooks/                      # useAuth, useChatStream, useConversations
├── theme/                      # theme.ts (palette + style helpers)
├── utils/                      # storage.ts, errors.ts, theme.ts
├── types/                      # shared TypeScript types
└── App.tsx                     # routes + providers (admin pages lazy-loaded)
```

## Key patterns

### Service layer
All API calls live in `services/`. `api.ts` holds a shared Axios instance whose request interceptor injects `Authorization: Bearer <token>` and whose response interceptor handles 401 by logging out. This keeps a single source of truth for endpoints, auth, and error handling, and makes mocking easy in tests.

### Custom hooks for server state
TanStack Query hooks in `hooks/` provide caching, background refetching, and loading/error states. For example, `useConversations(enabled)` only fetches once the user is authenticated.

### Centralized utilities
- **`utils/storage.ts`**: single token store across localStorage (persistent) and sessionStorage (session-only), with `getToken` / `setToken` / `removeToken`.
- **`utils/theme.ts`**: color palette and reusable MUI `sx` style objects (`darkTextFieldStyles`, `darkDialogStyles`, `darkButtonStyles`, `darkCheckboxStyles`), so styling stays consistent.
- **`utils/errors.ts`**: `getErrorMessage()` extracts a user-friendly message from Axios errors, strings, or objects.

### Authentication flow
- "Remember me" stores the token in localStorage; otherwise sessionStorage. `storage.getToken()` checks both.
- On mount the app reads the token and validates it against `/auth/me`; valid tokens populate `AuthContext`, invalid ones are cleared.
- `ProtectedRoute` guards the `/admin` routes and enforces the admin role.

### Form validation
react-hook-form with Zod resolvers:

```typescript
const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(6, 'Password must be at least 6 characters'),
});
```

### Streaming chat
Server-Sent Events over the native `fetch` API (Axios does not stream well):

1. The user message is added to the UI immediately.
2. `useChatStream` opens the SSE connection to `/chat/stream` with a manual bearer header.
3. The backend streams token-by-token; the UI appends chunks in real time.
4. The streaming message is held in separate state from the persisted history to avoid flicker, then committed on completion (with any RAG citations).

## Components

- **MessageList / MessageItem**: render the conversation; `MessageItem` renders Markdown via `react-markdown` + `remark-gfm`, plus image/document attachments and RAG citation chips.
- **MessageInput**: multi-line input, Enter to send (Shift+Enter for newline), with image and document attach.
- **Sidebar**: conversation history, new chat, theme toggle, and the user/admin menu.
- **ChatPage**: orchestrates the chat view.
- **AdminDashboard / AdminDocumentsPage**: admin overview and the RAG document upload/list/delete UI (lazy-loaded).

## State management

- **Local state** (`useState`) for UI concerns: modals, inputs, toggles.
- **Server state** (TanStack Query) for conversations, messages, documents, and user data.
- **Global state** via `AuthContext` (current user) and `ThemeContext` (theme), kept intentionally small.

## Styling

A dark theme is enforced through the palette in `theme/theme.ts` and a `ThemeProvider` wrapping the app. Components style via the `sx` prop using the shared style objects in `utils/theme.ts`, so look-and-feel stays consistent without repeating styles.

## Performance

- TanStack Query caching avoids redundant requests.
- Admin pages are code-split with `React.lazy` + `Suspense`.
- Streaming uses incremental DOM updates rather than re-rendering the whole history.

## Security

- JWT held in web storage (XSS risk acknowledged for a demo); expiration enforced by the backend.
- Zod validates all form input; Markdown rendering does not inject raw HTML.
- The backend owns CORS and authorization.

## Testing

Vitest + React Testing Library cover utilities, hooks, and components (for example `ProtectedRoute.test.tsx`, `chatService.test.ts`, `storage.test.ts`, `errors.test.ts`). The repo's `scripts/screenshots` Playwright tool drives the live app to regenerate the README screenshots. CI runs lint + tests + build on every push.

## Build

```bash
npm run dev      # Vite dev server on :5173
npm run build    # type-check + production build to dist/
npm run preview  # preview the production build
```

The backend URL comes from `VITE_API_BASE_URL` (`frontend/.env` locally; `.env.production` is written by `infra/deploy.ps1` at deploy time).
