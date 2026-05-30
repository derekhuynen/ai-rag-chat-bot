# Screenshot capture

Regenerates the README marketing screenshots (`images/pic1.png`–`pic5.png`) from a
running deployment, using Playwright. Useful so the screenshots can be refreshed
with clean demo data whenever the UI changes.

## What it captures

| File | Screen |
|------|--------|
| `pic1.png` | Chat UI (hero) |
| `pic2.png` | Admin Dashboard |
| `pic3.png` | Admin Documents |
| `pic4.png` | RAG answer with citations |
| `pic5.png` | Markdown & code rendering |

The capture order matches these README captions.

## Prerequisites

1. A running deployment (see [`../../infra/README.md`](../../infra/README.md)).
2. The demo corpus seeded (`infra/seed-demo.ps1`) so the chat has documents to cite,
   otherwise `pic4` will have no `Sources:` chips.
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
| `OUT_DIR` | `../../images` | where PNGs are written |
| `HERO_PROMPT` | "Tell me about projects that use AI." | first chat prompt |
| `CODE_PROMPT` | (a "run locally" prompt) | second chat prompt (for code rendering) |

On failure it writes `capture-error.png` to the output folder to aid debugging.
The browser binary and `node_modules/` are gitignored.
