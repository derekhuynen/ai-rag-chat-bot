---
applyTo: '**/*.tsx'
---

Dont over over use useState or useEffect. Only use them when necessary.

Use React query for data fetching and caching data. Use invalidation and refetching as needed. optimize perfomance
Use the React Context API for cross-cutting app state (see `AuthContext` and `ThemeContext` in `frontend/src/contexts/`). Do not use redux.
Use MUI for UI components and styling. Use the sx prop for custom styles. Do not use styled components or css modules.

Use React Router for routing. Use nested routes and route params as needed. Do not use any other routing library. use react-router-dom v7 or later. for lazy loading.
Use TypeScript for type safety. Define types and interfaces as needed. Do not use any or unknown types. Use strict mode in tsconfig.json.

Use the MUI MCP Server installed on vscode workspace
for best practices and patterns when using MUI v7
