# Contributing to AI_RAG_Chat_Bot

Thanks for your interest in contributing! This is a reference / demo project, but
issues and pull requests are welcome.

## Prerequisites

- **.NET SDK** 10.0+ and **Azure Functions Core Tools** v4 (backend)
- **Node.js** 20+ (frontend)
- Azure resources (Cosmos DB, Storage, Azure OpenAI, Azure AI Search) — see the
  [README](README.md#resources-required). The app is keyless and authenticates via
  `DefaultAzureCredential`, so run `az login` locally.

## Local setup

```bash
# Backend
cd backend
cp local.settings.example.json local.settings.json   # fill in endpoints (no keys)
dotnet restore
func start

# Frontend (in another terminal)
cd frontend
npm install
echo VITE_API_BASE_URL=http://localhost:7071/api > .env
npm run dev
```

## Before you open a PR

Run the same checks CI runs and make sure they pass:

```bash
# Backend: build + tests
dotnet build AI_RAG_Chat_Bot.sln -c Release
dotnet test  AI_RAG_Chat_Bot.sln -c Release

# Frontend: lint + tests + build
cd frontend
npm run lint
npm run test
npm run build
```

Please add or update tests for any behavior you change.

## Conventions

- **Branches:** work on a feature branch (e.g. `feat/...`, `fix/...`), not directly on `main`.
- **Commits:** use [Conventional Commits](https://www.conventionalcommits.org/)
  prefixes (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`).
- **Secrets:** never commit `local.settings.json`, `.env` files, or any keys —
  they are gitignored. The app is designed to be keyless; keep it that way.
- **Security:** validate and parameterize anything that touches user input
  (Cosmos queries use parameterized `QueryDefinition`); don't return raw
  exception messages to clients.

## Reporting issues

Open a GitHub issue with steps to reproduce, expected vs. actual behavior, and
your environment (OS, .NET/Node versions). For security-sensitive reports,
please avoid filing a public issue with exploit details.
