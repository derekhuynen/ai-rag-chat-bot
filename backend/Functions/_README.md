# Functions overview

Each HTTP-triggered function is a thin controller: it validates auth, calls a
service, and shapes the response. One feature area per function class.

- `AuthFunction`: register, login, current user (`/auth/*`).
- `AdminFunction`: admin stats and user list (`/management/stats`, `/management/users`).
- `ConversationFunction`: conversation CRUD (`/conversations`).
- `ChatStreamFunction`: streaming chat over SSE (`/chat/stream`).
- `ChatFunction`: non-streaming chat (`/chat`), optional.
- `ImageUploadFunction`: image upload for chat (`/image/upload`).
- `DocumentUploadFunction`: in-chat document attachment upload (`/document/upload`).
- `DocumentManagementFunction`: admin RAG document upload, list, get, download,
  and delete (`/management/documents/*`). Upload runs the full processing
  pipeline (chunk, summarize, embed, index, blob move) **synchronously** before
  responding, so a document is queryable as soon as the call returns `processed`.
- `HealthCheckFunction`: health probe (`/health`).
