# Project: SmartDesk Support Assistant (2024)

> Sample demo document. Fictional Contoso project. Not real.

## Project

SmartDesk is an AI customer-support assistant built into Contoso Desk. It drafts
replies to incoming support tickets by retrieving relevant help-center articles
and past resolved tickets, then summarizing a suggested response for a human
agent to review and send.

## How AI is used

- Uses a large language model to read the incoming ticket and draft a reply.
- Retrieves relevant knowledge-base articles using hybrid (keyword + vector)
  search, so answers are grounded in Contoso's own documentation.
- Surfaces clickable citations back to the source articles for every draft.

## Tech highlights

- **Frontend:** React + TypeScript, embedded as a side panel in Contoso Desk.
- **Backend:** Serverless functions for retrieval and generation.
- **Data / Search:** Document store plus a hybrid search index for retrieval.
- **AI:** Managed cloud LLM for chat, small embedding model for vectors.

## Workflow

1. A customer submits a support ticket.
2. The assistant retrieves the most relevant articles and prior tickets.
3. The model drafts a grounded reply with citations.
4. A support agent reviews, edits if needed, and sends.

## Results

- Median first-response drafting time dropped from minutes to seconds.
- Agents kept full control: every reply is human-approved before sending.
- Citation links reduced "where did this come from?" follow-up questions.

## Challenges and solutions

- **Hallucinated answers** - solved by grounding every reply in retrieved
  documents and showing citations.
- **Stale knowledge base** - solved by re-indexing articles on publish.
- **Cost control** - solved by using a small embedding model and caching.
