# Screenshot capture

Regenerates the README marketing screenshots in `docs/images/` from a running
deployment, using Playwright. Useful so the screenshots can be refreshed with
clean demo data whenever the UI changes.

## What it captures

| File | Screen |
|------|--------|
| `chat-streaming.png` | Streaming chat (hero) |
| `rag-citations.png` | RAG answer with citations |
| `markdown-rendering.png` | Markdown and code rendering |
| `admin-dashboard.png` | Admin dashboard |
| `admin-documents.png` | Document library |

Each file is named for the screen it shows, and the captions in the top-level
README match.

## Prerequisites

1. A running deployment (see [`../../infra/README.md`](../../infra/README.md)).
2. The demo corpus seeded (`infra/seed-demo.ps1`) so the chat has documents to cite,
   otherwise `rag-citations.png` will have no `Sources:` chips.
3. Node 20+.

## Run

```powershell
cd scripts/screenshots
npm install            # installs Playwright + the chromium browser

$env:SPA_URL = "https://<web endpoint from deploy.ps1>"
$env:ADMIN_PASSWORD = "<admin password>"
npm run capture
```

By default it also registers a neutral `demo@example.com` user so the admin user
table has a second row. Set `CREATE_DEMO_USER=false` to skip that.

### Configuration (env vars)

| Var | Default | Notes |
|-----|---------|-------|
| `SPA_URL` | (required) | SPA static-website URL |
| `ADMIN_PASSWORD` | (required) | admin password from `deploy.ps1` |
| `ADMIN_EMAIL` | `admin@example.com` | admin login |
| `ADMIN_NAME` | `Administrator` | sidebar display name |
| `CREATE_DEMO_USER` | `true` | register a demo user for the table |
| `DEMO_EMAIL` / `DEMO_PASSWORD` / `DEMO_NAME` | `demo@example.com` / `DemoPassword123!` / `Demo User` | demo user |
| `OUT_DIR` | `../../docs/images` | where PNGs are written |
| `HERO_PROMPT` | "Tell me about projects that use AI." | first chat prompt |
| `CODE_PROMPT` | (a "run locally" prompt) | second chat prompt (for code rendering) |

On failure it writes `capture-error.png` to the output folder to aid debugging.
The browser binary and `node_modules/` are gitignored.
