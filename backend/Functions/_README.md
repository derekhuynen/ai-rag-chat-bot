# Functions Overview

- `DocumentManagementFunction`
  - Handles admin document upload, listing, retrieval, deletion, and download.
  - Uploads now perform **synchronous** document processing (chunking, summarization, embeddings, AI Search indexing, and blob move) immediately after saving the document record in Cosmos DB.
- `DocumentProcessingFunction`
  - Currently unused placeholder after refactor. Original queue-trigger-based implementation was removed in favor of synchronous processing in `DocumentManagementFunction.UploadDocument`.
- `QueueSmokeTestFunction`
  - Temporary queue smoke test used during debugging has been removed.

This keeps the backend aligned with the project guidelines: one feature per function class and no unused test/debug functions in production code.
