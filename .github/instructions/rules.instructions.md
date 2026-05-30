---
applyTo: '**'
---

Dont over generate code. Only generate what is necessary to fulfill the user request.
All mark down files should be 300 lines or less. Except the Main Readme.md which can be 600 lines.
im using bash terminal

run bash command like this: (cd frontend && npm run dev)
full path wrapped in ()

## Page Component Organization Rules

**CRITICAL: Avoid duplicate page files at all costs!**

### Single Source of Truth

- Each page component must exist in ONLY ONE location
- Before creating a new page, check if it already exists elsewhere
- Always verify which file is imported in `frontend/src/App.tsx`

### Folder Structure Guidelines

**Use `frontend/src/pages/` for:**

- Cross-feature pages (LandingPage, HomePage, ExplorePage, FavoritesPage)
- Pages that don't belong to a specific feature domain
- Public marketing pages

**Use `frontend/src/features/{featureName}/pages/` for:**

- Feature-specific pages (TripsPage, TripDetailPage, CreateTripPage, EditTripPage)
- Pages tightly coupled to a specific feature domain
- Pages that use feature-specific components extensively

### When Creating a New Page

1. **Check existing locations** - Search for similar page names in both `pages/` and `features/*/pages/`
2. **Decide location** - Is it feature-specific or cross-cutting?
3. **Create in ONE place only** - Never create in both locations
4. **Import in router** - Add the correct import path to `frontend/src/App.tsx`
5. **Verify** - Test the route loads from the correct file

### When Refactoring Pages

- If moving a page from `pages/` to `features/`, DELETE the old file
- Update the import in `frontend/src/App.tsx` immediately
- Never leave both versions of the same page

### Red Flags (Check for duplicates if you see these)

- ⚠️ Two files with the same component name in different folders
- ⚠️ Pages not rendering changes after editing
- ⚠️ Confusion about which file to edit
- ⚠️ Build warnings about duplicate exports

**ALWAYS check `frontend/src/App.tsx` to see which file is actually being used!**

Any documents that are created go in copilot_documents

Do no generate endless markdown files for progress. Only generate them when asked too.
