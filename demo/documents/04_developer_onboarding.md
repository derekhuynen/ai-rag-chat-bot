# Contoso Engineering - Developer Onboarding

> Sample demo document. Fictional Contoso handbook. Not real.

Welcome to Contoso Engineering. This guide gets a new developer productive in
their first week.

## Day one

1. Get access to the source repositories.
2. Install the toolchain (Node.js, the .NET SDK, and the cloud CLI).
3. Authenticate to cloud resources with the CLI - Contoso is keyless, so you log
   in once and never copy access keys around.

## Running a service locally

Most backend services are serverless functions. To run one locally:

```bash
# 1) Backend
cd backend
cp local.settings.example.json local.settings.json   # fill in endpoint URLs
dotnet restore
func start                                            # http://localhost:7071

# 2) Frontend (new terminal)
cd ../frontend
npm install
npm run dev                                           # http://localhost:5173
```

## Coding standards

- TypeScript runs in strict mode; avoid `any` unless justified.
- Match the surrounding style instead of introducing new conventions.
- Prefer the smallest change that fully solves the task.

## Pull requests

- Branch names follow `type/short-description` (for example `feat/dark-mode`).
- Keep commits small and atomic; one logical change each.
- CI runs build, lint, and tests on every push; do not merge red builds.

## Security basics

- Never commit secrets, tokens, or credentials.
- Use managed identity, not stored keys, for cloud access.
- Report anything that looks like a leaked secret immediately.
