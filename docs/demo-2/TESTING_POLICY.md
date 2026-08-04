# Testing Policy

## 1. Purpose and Objectives

This document defines the standards, procedures, and responsibilities for all testing done across OptiLifts. The policy's goal is to ensure that all changes done to any of our three layers - frontend, core API, or AI service - will be thoroughly verified before it `dev` or `main`. This ensures that our deployed system remains reliable and correct.

Why do we Test in OptiLift?
- Catch any defects before they reach a shared branch or a demo.
- Allows for teammates to refactor and extend code that has been backed by tests. Also the suite informs teammates when tests fails due to behaviour changing unexpectedly.
- Provide solid proof that our functionality works end-to-end against a real database and a real browser.

## 2. Testing Types

OptiLifts uses multiple different types of testing combined, that is run on the CI/CD pipeline to ensure that every pull request is checked properly before it can be merged.

| Type | Scope | What it verifies |
| :--- | :--- | :--- |
| **Unit** | An individual class, handler, or component that is tested in isolation with mocked dependencies. | Logic correctness at a granulated and singular level. |
| **Integration** | Real HTTP request through the ASP.NET Core pipeline with a real PostgreSQL test container. | That controllers, MediatR handlers, and EF Core mappings all work together properly against a real database. |
| **End-to-End (E2E)** | A real browser (via Playwright) testing the actual frontend, backend and database using Docker Compose. | That complete user interactions will work as intended. |
| **User Acceptance Testing (UAT)** | Manually with clients and mentor | That delivered features meet the client's needs and follow proper procedure according to clients and mentor. |

## 3. Tools and Environments

**.NET Core API:**
- xUnit (runs each test)
- Moq (fakes services so they don't hit real ones like blob storage)
- Testcontainers (Creates real Postgres database in Docker, shared by all integration tests)
- Respawn (wipes the database clean before every test)
- FluentAssertions (convenient formatting of test checker)

**React Frontend:**
- Vitest (runs each test)
- React Testing Library (renders components and simulates clicks or typing)

**Python AI API:**
- pytest (runs each test)
- httpx (fake requests are sent straight into FastAPI)

**End-to-End:**
- Playwright (drives a real browser through the app)
- Docker Compose (runs the entire stack and mimics production)

**Code Coverage:**
- Coveralls (collects all coverage percentages for display)
- `@vitest/coverage-v8` (measures frontend coverage)
- coverlet (measures backend coverage)
- `pytest-cov` (measures AI API coverage)


**Environments:**
- **Local** - we run unit and integration tests using `pnpm test` and end-2-end tests against using `pnpm test:e2e`.
- **CI** - every push and pull request to `main` or `dev` will  run the entire suite (lint, unit, integration, and E2E) via GitHub Actions. Only allowing to merge after successfully running all tests.

## 4. Defect Management

Defects are tracked as GitHub Issues in this repository, labelled by area (`frontend`, `backend`, `testing`, `documentation`, `devops`, etc.) and by type (`bug` for defects). Each issue links back to the pull request that introduces the fix.

When a defect is found:
1. It is first discussed with team members in a meeting. The task of fixing it is distributed to an available member in which they will create a ticket for it.
2. The assigned member will then fix the issue at hand and create a test to ensure that the fix is successful.
3. The fix is then as a pull requested with a reference to the issue, following the branching and PR conventions in [`docs/REPO.md`](REPO.md).
4. CI is then run automatically on that pull request and only after it passes, the PR can be merged.

## 5. Acceptance Criteria

A feature or fix is considered ready for acceptance when:
- All logic has unit test coverage for its core decision paths.
- Any new or adjusted API endpoint must have integration test coverage that tests the endpoint against a real database.
- Any new or adjusted user-interactive functionality that's part of a core use case has E2E coverage.
- The feature must be manually tested against a locally running stack using `pnpm prod` before a PR is opened.
- Our full CI pipeline - which includes lint, unit, integration, E2E and building - must pass on the PR.
- Two team members must review the PR and do quality control on the PR to ensure everything is correct.

## 6. Roles and Responsibilities

- **Feature owner** - They write the unit and integration tests for the feature they introduce, along with creating the issues and PR involving their feature. They are in charge of ensuring that the tests are behaving are intended and are testing correctly and thoroughly.
- **PR reviewer** - Checks that the tests actually exercise the changed behaviour and that there are no vulnerabilities or weaknesses in the feature owner's changes.
- **Team lead** - Ensures the branching strategy and rules are respected, and that our `main` branch always contains a working and fully tested version of the codebase.
- **Industry mentor / lecturer mentor** - Perform UAT during reviews and meetings by evaluating our decision and features based on their needs.
