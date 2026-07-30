# Coding Standards Document

## Purpose

This document defines the coding conventions used across OptiLifts to maintain uniformity, clarity, flexibility, reliability, and efficiency. Because the repository is a multi-stack monorepo, the standards below cover the frontend, the .NET core backend, end-to-end testing, and infrastructure code.

## Repository Structure

The repository is organised by delivery surface so each major application area has a clear ownership boundary.

```text
OptiLifts/
├── backend/
│   ├── ai-api/
│   └── core-api/
│       ├── OptiLifts.API/
│       ├── OptiLifts.Application/
│       ├── OptiLifts.Domain/
│       ├── OptiLifts.Infrastructure/
│       └── OptiLifts.Tests/
├── docs/
├── e2e/
├── frontend/
│   ├── public/
│   └── src/
│       ├── components/
│       ├── constants/
│       ├── context/
│       ├── lib/
│       ├── pages/
│       ├── types/
│       └── __tests__/
├── images/
├── infrastructure/
├── scripts/
├── package.json
├── pnpm-workspace.yaml
├── docker-compose.yml
└── docker-compose.prod.yml
```

## Structural Conventions

### Monorepo organisation

- Shared workflow commands must be defined at the repository root whenever they configure multiple services.
- Application-specific implementation must remain inside its folder rather than being placed at the root.
- Documentation belongs in `docs/` and deployment definitions belong in `infrastructure/`.

### Backend layering

- The .NET backend must preserve the existing layered separation between API, Application, Domain, Infrastructure, and Tests.
- `OptiLifts.Domain` should contain core business entities, value objects, and domain rules without infrastructure dependencies.
- `OptiLifts.Application` should contain use-case configuration, service contracts, and application logic.
- `OptiLifts.Infrastructure` should contain database, persistence, external integrations, and framework-specific implementations.
- `OptiLifts.API` should contain HTTP endpoints, request/response mapping, and application composition.

### Frontend structure

- Reusable UI belongs in `frontend/src/components/`.
- Route-level screens belong in `frontend/src/pages/`.
- Shared types belong in `frontend/src/types/`.
- Constants, helpers, and lightweight utilities belong in `frontend/src/constants/` and `frontend/src/lib/`.
- Frontend tests should be kept in `frontend/src/__tests__/` or adjacent to the feature when strong locality improves readability.

## General Coding Conventions

### Naming

- Use descriptive names that communicate intent instead of abbreviations.
- Use `PascalCase` for classes, React components, C# types, and TypeScript type aliases.
- Use `camelCase` for variables, functions, method names, and object properties.
- Use `UPPER_SNAKE_CASE` only for true constants or environment-level configuration values.
- Use `kebab-case` for branch names, Docker-related filenames, and general folder naming where appropriate.

### Readability and maintainability

- Keep functions and methods focused on a single responsibility.
- Prefer explicit, readable control flow over compact but unclear logic.
- Avoid duplicated business logic across frontend and API services.
- Keep public interfaces stable and make changes at the owning abstraction instead of patching behavior at call sites.

### Error handling

- Validate external input at service boundaries.
- Fail fast on invalid state rather than silently ignoring errors.
- Return actionable API errors and avoid exposing internal implementation details.
- Log useful diagnostic information without logging secrets or sensitive personal data.

## Language-Specific Standards

### TypeScript and React

- Use TypeScript for all new frontend logic.
- Prefer functional React components.
- Name component files with PascalCase when the file exports a component.
- Keep presentational concerns in components

### C# and .NET

- Use file-scoped namespaces.
- Use four spaces(tab) for indentation.
- Keep `using` directives organised with system namespaces first.
- Place each public type in its own file where practical.
- Keep controllers thin and move business logic into the application layer.

## Testing and Quality Standards

- Every new feature or bug fix should include automated tests where the behavior can be validated reliably.
- Frontend unit and component tests should use Vitest.
- Backend automated tests should use the OptiLifts.Tests project and run through dotnet test.
- Cross-application user journeys should be covered with Playwright tests in `e2e/`.
- Code should be linted and tested before merge.

## Configurations That Enforce Consistency

The following repository configurations are used to maintain coding style consistency.

### Root workspace scripts

The root package.json provides standardised commands so the team use the same build, lint, and test entry points.

- `pnpm lint` runs frontend ESLint, backend dotnet format --verify-no-changes, and AI API Ruff checks.
- `pnpm lint:fix` runs the corresponding auto-fix commands.
- `pnpm test` runs frontend, backend, and AI API automated tests.
- `pnpm test:e2e` runs Playwright end-to-end tests.
- `pnpm build` builds the frontend and the .NET backend.

### Frontend ESLint configuration

The frontend uses `frontend/eslint.config.js` to enforce consistent JavaScript and TypeScript quality rules.

- ESLint applies to `ts` and `tsx` files.
- The configuration extends recommended rules from core ESLint, `typescript-eslint`, `eslint-plugin-react-hooks`, and Vite React refresh support.

### Backend EditorConfig configuration

The .NET backend uses `backend/core-api/.editorconfig` to enforce formatting and style expectations.

- Line endings are set to `lf`.
- Indentation uses four spaces(tab).
- System `using` directives are sorted first.
- Namespace declarations must be file-scoped.
- EF Core migrations are excluded from analyser enforcement because they are generated code.

### End-to-end and infrastructure conventions

- Playwright tests are stored in `e2e/` and should be named to reflect the user flow they validate.
- Infrastructure definitions are written in TypeScript under `infrastructure/` and should follow the same TypeScript naming and linting expectations used elsewhere in the workspace.

## Source Control Standards

To keep history consistent with the repository workflow:

- Branches must follow the domain/name-of-branch convention.
- Commit messages should use the project prefixes such as feature:, fix:, test:, and docs:.
- All changes must be reviewed through pull requests rather than direct commits to shared branches.

## Summary

These standards ensure OptiLifts code remains understandable, consistent, and maintainable across a mixed technology stack. Contributors should treat the repository structure, linting tools, naming conventions, and test commands in this document as the default baseline for all future development.