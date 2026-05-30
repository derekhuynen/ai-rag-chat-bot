# Demo content

Sample data for trying out the AI RAG Chat Bot without uploading anything real.

## `documents/`

Five fictional **Contoso Ltd.** markdown files. Contoso is the standard
made-up company used in Microsoft samples, so nothing here references a real
person, client, or project:

| File | What it demos |
|------|---------------|
| `01_contoso_overview.md` | Company overview; ties the other docs together |
| `02_smartdesk_support_ai.md` | An AI customer-support assistant project |
| `03_inventory_forecasting.md` | An ML demand-forecasting project |
| `04_developer_onboarding.md` | Engineering handbook (includes a code block) |
| `05_doc_generation_ai.md` | An AI proposal-generation proof of concept |

Together they let the chat answer questions like *"tell me about projects that
use AI"* with grounded, multi-source citations.

## Seeding a deployment

Documents are grounded by uploading them through the admin UI; there is no
pre-seeded corpus. To load this set automatically, use the seed script after
deploying (see [`../infra/README.md`](../infra/README.md)):

```powershell
cd infra
./seed-demo.ps1 -ApiBaseUrl <API base URL from deploy.ps1> `
                -AdminPassword (Read-Host "Admin password" -AsSecureString)
```

The script logs in as the admin user and uploads every `.md`/`.txt` file in
`demo/documents/`. The backend chunks, summarizes, embeds, and indexes each file
into Azure AI Search synchronously, so they are queryable as soon as the script
finishes.

You can also upload them by hand: log in as admin, open **Admin -> Documents**,
and use **Select .txt or .md document**.

## Try these prompts

- "Tell me about projects that use AI."
- "How does SmartDesk avoid hallucinated answers?"
- "Summarize how a new developer runs a service locally."
- "What does the inventory forecasting project do about privacy?"
