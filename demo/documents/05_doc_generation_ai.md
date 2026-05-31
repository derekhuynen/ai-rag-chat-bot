# Project: AI Proposal Generation (2024)

> Sample demo document. Fictional Contoso proof of concept. Not real.

## Project

An internal proof of concept for automating sales-proposal creation with AI. It
takes a short brief plus supporting notes and generates a first-draft proposal in
Contoso's standard format for a salesperson to refine.

## How AI is used

- Uses a large language model to analyze the brief and supporting notes.
- Generates proposal sections (summary, scope, timeline, pricing outline) in an
  approved company template.
- Exports the draft as both PDF and a Word document.

## Tech highlights

- **Frontend:** React with a simple upload-and-review flow.
- **Backend:** Serverless functions for parsing and generation.
- **Data / Storage:** Blob storage for uploads and generated files; a queue for
  background processing.
- **AI:** Managed cloud LLM for drafting.

## Automations and workflow

- Users upload supporting documents through a web UI.
- Background workers parse, chunk, and prepare the data for the model.
- The model drafts the proposal, which is formatted into deliverables.
- End-to-end delivery is automated: upload to processing to draft to export.

## Key takeaways

- AI-driven drafting can speed up previously manual, error-prone document work.
- Integration with cloud storage and queues enables scalable, event-driven
  processing.
- Prompt design and data preprocessing are critical for consistent output.
- A human always reviews and approves the final proposal before it is sent.

## Challenges and solutions

- **Diverse, messy input formats** - solved with robust preprocessing.
- **Inconsistent output** - solved by iterating on prompts and strict templates.
