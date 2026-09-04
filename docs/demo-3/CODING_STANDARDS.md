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
- `OptiLifts.Domain` should contain core business entities, value objects, domain events, and domain rules without infrastructure or framework dependencies.
- `OptiLifts.Application` should contain use-case configuration, service contracts, commands/queries, DTOs, and application logic.
- `OptiLifts.Infrastructure` should contain database access (EF Core), persistence configurations, external service integrations, and framework-specific implementations.
- `OptiLifts.API` should contain HTTP endpoints, controller definitions, request/response mapping, middleware, and dependency injection composition.

### Frontend structure

- Reusable UI belongs in `frontend/src/components/`.
- Route-level screens belong in `frontend/src/pages/`.
- Shared types belong in `frontend/src/types/`.
- Constants, helpers, and lightweight utilities belong in `frontend/src/constants/` and `frontend/src/lib/`.
- Frontend tests should be kept in `frontend/src/__tests__/` or adjacent to the feature when strong locality improves readability.

## General Coding Conventions

### Naming

- Use descriptive names that communicate intent instead of abbreviations.
- Use `PascalCase` for classes, React components, C# types, and TypeScript type aliases/interfaces.
- Use `camelCase` for variables, functions, method names, and object properties.
- Use `UPPER_SNAKE_CASE` only for true constants or environment-level configuration values.
- Use `kebab-case` for branch names, Docker-related filenames, and general folder naming where appropriate.

#### Naming examples

```typescript
//PascalCase for types and components
interface LiftTelemetryProps {
  workoutSessionId: string;
}

export function WorkoutTrackerCard({ workoutSessionId }: LiftTelemetryProps) {
  //camelCase for variables and functions
  const calculatedOverloadWeight = 85.5;

  function calculateTargetReps(currentRpe: number): number {
    return currentRpe > 8 ? 6 : 8;
  }

  return <div>{calculatedOverloadWeight}</div>;
}

//UPPER_SNAKE_CASE for constants
export const MAX_RETRY_ATTEMPTS = 3;
export const DEFAULT_REST_INTERVAL_SECONDS = 90;
```

```csharp
//PascalCase for C# classes, records, and methods
namespace OptiLifts.Domain.Entities;

public record ExerciseMetrics(int TargetReps, decimal WeightKg);

public class WorkoutSession
{
    //UPPER_SNAKE_CASE for constant values
    public const int MaxAllowedSets = 20;

    //camelCase for parameters and local variables
    public void RecordSet(int setNumber, decimal weightKg)
    {
        var isWithinRange = weightKg > 0 && setNumber <= MaxAllowedSets;
        if (!isWithinRange)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Weight must be positive.");
        }
    }
}
```

### Readability and maintainability

- Keep functions and methods focused on a single responsibility.
- Prefer explicit, readable control flow over compact but unclear logic.
- Avoid duplicated business logic across frontend and API services.
- Keep public interfaces stable and make changes at the owning abstraction instead of patching behavior at call sites.

#### Guard clauses and early exit example

```csharp
//Avoid deep nesting
public void CompleteSet(Guid sessionId, int setNumber, decimal weight)
{
    if (sessionId != Guid.Empty)
    {
        var session = _repository.GetById(sessionId);
        if (session != null)
        {
            if (session.IsActive)
            {
                session.AddCompletedSet(setNumber, weight);
            }
        }
    }
}
```

### Error handling

- Validate external input at service boundaries.
- Fail fast on invalid state rather than silently ignoring errors.
- Return actionable API errors and avoid exposing internal implementation details.
- Log useful diagnostic information without logging secrets or sensitive personal data.

#### RFC 7807 problem details response example

```json
{
  "type": "https://errors.optilifts.com/validation-error",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request payload failed domain validation checks.",
  "instance": "/api/v1/sessions/4f8b1c20/sets",
  "errors": {
    "WeightKg": ["Weight must be greater than 0 kg."]
  }
}
```

#### Structured logging example

```csharp
//Avoid adding variable into strings
_logger.LogError($"Failed to calculate progressive overload for user {userId}: {ex.Message}");

//Structure message templates with named parameters
_logger.LogError(
    ex,
    "Failed to calculate progressive overload for User {UserId} in Session {SessionId}",
    userId,
    sessionId);
```

## Language-Specific Standards

### TypeScript and React

- Use TypeScript for all new frontend logic.
- Prefer functional React components.
- Name component files with PascalCase when the file exports a component.
- Keep presentational concerns in components.

#### Component and hook separation example

```tsx
//frontend/src/lib/hooks/useProgressiveOverload.ts
import { useState, useEffect } from "react";
import { OverloadRecommendation } from "../../types/workout";

export function useProgressiveOverload(exerciseId: string) {
  const [data, setData] = useState<OverloadRecommendation | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    let isMounted = true;
    setIsLoading(true);

    fetch(`/api/v1/exercises/${exerciseId}/recommendation`)
      .then((res) => {
        if (!res.ok) throw new Error("Failed to load recommendation");
        return res.json();
      })
      .then((recommendation: OverloadRecommendation) => {
        if (isMounted) setData(recommendation);
      })
      .catch((err: Error) => {
        if (isMounted) setError(err);
      })
      .finally(() => {
        if (isMounted) setIsLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [exerciseId]);

  return { data, isLoading, error };
}
```

```tsx
//frontend/src/components/RecommendationCard.tsx
import React from "react";
import { useProgressiveOverload } from "../lib/hooks/useProgressiveOverload";

interface RecommendationCardProps {
  exerciseId: string;
}

export const RecommendationCard: React.FC<RecommendationCardProps> = ({ exerciseId }) => {
  const { data, isLoading, error } = useProgressiveOverload(exerciseId);

  if (isLoading) {
    return <div className="card-skeleton" aria-busy="true">Loading target recommendations...</div>;
  }

  if (error || !data) {
    return <div className="alert alert-error">Unable to retrieve progressive overload data.</div>;
  }

  return (
    <div className="recommendation-card">
      <h3 className="text-lg font-semibold">{data.exerciseName}</h3>
      <p className="text-sm">Target Weight: {data.suggestedWeightKg} kg</p>
      <p className="text-sm">Target Reps: {data.suggestedReps}</p>
    </div>
  );
};
```

### C# and .NET

- Use file-scoped namespaces.
- Use four spaces (tab) for indentation.
- Keep `using` directives organised with system namespaces first.
- Place each public type in its own file where practical.
- Keep controllers thin and move business logic into the application layer.

#### C# controller and application handler example

```csharp
//backend/core-api/OptiLifts.API/Controllers/WorkoutsController.cs
namespace OptiLifts.API.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OptiLifts.Application.Workouts.Commands;
using OptiLifts.Application.Workouts.DTOs;

[ApiController]
[Route("api/v1/workouts")]
public class WorkoutsController : ControllerBase
{
    private readonly IRecordSetCommandHandler _handler;

    public WorkoutsController(IRecordSetCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("{id:guid}/sets")]
    [ProducesResponseType(typeof(WorkoutSetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordSet(
        [FromRoute] Guid id,
        [FromBody] RecordSetRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RecordSetCommand(id, request.ExerciseId, request.Reps, request.WeightKg);
        var response = await _handler.HandleAsync(command, cancellationToken);

        return Ok(response);
    }
}
```

## Testing and Quality Standards

- Every new feature or bug fix should include automated tests where the behavior can be validated reliably.
- Frontend unit and component tests should use Vitest.
- Backend automated tests should use the OptiLifts.Tests project and run through dotnet test.
- Cross-application user journeys should be covered with Playwright tests in `e2e/`.
- Code should be linted and tested before merge.

### Frontend component test example (Vitest & React Testing Library)

```tsx
//frontend/src/__tests__/RecommendationCard.test.tsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { RecommendationCard } from "../components/RecommendationCard";
import * as hooks from "../lib/hooks/useProgressiveOverload";

describe("RecommendationCard", () => {
  it("renders suggested weight and reps when data is loaded", () => {
    vi.spyOn(hooks, "useProgressiveOverload").mockReturnValue({
      data: {
        exerciseName: "Barbell Bench Press",
        suggestedWeightKg: 100,
        suggestedReps: 5,
      },
      isLoading: false,
      error: null,
    });

    render(<RecommendationCard exerciseId="ex-101" />);

    expect(screen.getByText("Barbell Bench Press")).toBeInTheDocument();
    expect(screen.getByText("Target Weight: 100 kg")).toBeInTheDocument();
    expect(screen.getByText("Target Reps: 5")).toBeInTheDocument();
  });
});
```

### Backend unit test example (xUnit & FluentAssertions)

```csharp
//backend/core-api/OptiLifts.Tests/Domain/WorkoutSessionTests.cs
namespace OptiLifts.Tests.Domain;

using System;
using FluentAssertions;
using OptiLifts.Domain.Entities;
using Xunit;

public class WorkoutSessionTests
{
    [Fact]
    public void RecordSet_WhenWeightIsNegative_ThrowsArgumentOutOfRangeException()
    {
        //arrange
        var session = new WorkoutSession(Guid.NewGuid(), DateTime.UtcNow);
        var invalidWeight = -5.0m;

        //act
        Action act = () => session.RecordSet(setNumber: 1, weightKg: invalidWeight);

        //assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("weightKg");
    }
}
```

### End-to-end test example (Playwright)

```typescript
//e2e/workout-logging-journey.spec.ts
import { test, expect } from "@playwright/test";

test.describe("Workout Logging Journey", () => {
  test("allows user to log a set and updates session overview", async ({ page }) => {
    await page.goto("/workouts/new");

    await page.getByLabel("Exercise").selectOption("Squat");
    await page.getByLabel("Weight (kg)").fill("120");
    await page.getByLabel("Reps").fill("5");
    await page.getByRole("button", { name: "Save Set" }).click();

    await expect(page.getByTestId("logged-sets-list")).toContainText("Squat: 120 kg x 5 reps");
  });
});
```

## Configurations That Enforce Consistency

The following repository configurations are used to maintain coding style consistency.

### Root workspace scripts

The root package.json provides standardised commands so the team use the same build, lint, and test entry points.

- `pnpm lint` runs frontend ESLint, backend dotnet format --verify-no-changes, and AI API Ruff checks.
- `pnpm lint:fix` runs the corresponding auto-fix commands.
- `pnpm test` runs frontend, backend, and AI API automated tests.
- `pnpm test:e2e` runs Playwright end-to-end tests.
- `pnpm build` builds the frontend and the .NET backend.

#### Workspace scripts configuration example

```json
{
  "name": "optilifts-monorepo",
  "private": true,
  "scripts": {
    "lint": "pnpm --filter frontend lint && dotnet format backend/core-api --verify-no-changes && ruff check backend/ai-api",
    "lint:fix": "pnpm --filter frontend lint:fix && dotnet format backend/core-api && ruff format backend/ai-api",
    "test": "pnpm --filter frontend test && dotnet test backend/core-api/OptiLifts.Tests && pytest backend/ai-api",
    "test:e2e": "playwright test -c e2e/playwright.config.ts",
    "build": "pnpm --filter frontend build && dotnet build backend/core-api/OptiLifts.API"
  }
}
```

### Frontend ESLint configuration

The frontend uses `frontend/eslint.config.js` to enforce consistent JavaScript and TypeScript quality rules.

- ESLint applies to `ts` and `tsx` files.
- The configuration extends recommended rules from core ESLint, `typescript-eslint`, `eslint-plugin-react-hooks`, and Vite React refresh support.

#### ESLint configuration example

```javascript
//frontend/eslint.config.js
import js from "@eslint/js";
import tseslint from "typescript-eslint";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";

export default tseslint.config(
  { ignores: ["dist", "node_modules"] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ["**/*.{ts,tsx}"],
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "react-refresh/only-export-components": ["warn", { allowConstantExport: true }],
      "@typescript-eslint/no-explicit-any": "error",
      "@typescript-eslint/explicit-function-return-type": "off",
    },
  }
);
```

### Backend EditorConfig configuration

The .NET backend uses `backend/core-api/.editorconfig` to enforce formatting and style expectations.

- Line endings are set to `lf`.
- Indentation uses four spaces (tab).
- System `using` directives are sorted first.
- Namespace declarations must be file-scoped.
- EF Core migrations are excluded from analyser enforcement because they are generated code.

#### EditorConfig configuration example

```ini
#backend/core-api/.editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
indent_size = 4
trim_trailing_whitespace = true
insert_final_newline = true
```

### End-to-end and infrastructure conventions

- Playwright tests are stored in `e2e/` and should be named to reflect the user flow they validate.
- Infrastructure definitions are written in TypeScript under `infrastructure/` and should follow the same TypeScript naming and linting expectations used elsewhere in the workspace.

#### Infrastructure definition example

```typescript
//infrastructure/src/stacks/database-stack.ts
export interface DatabaseStackConfig {
  instanceIdentifier: string;
  allocatedStorageGb: number;
  databaseName: string;
}

export function createDatabaseConfig(isProduction: boolean): DatabaseStackConfig {
  return {
    instanceIdentifier: isProduction ? "optilifts-db-prod" : "optilifts-db-dev",
    allocatedStorageGb: isProduction ? 100 : 20,
    databaseName: "optilifts",
  };
}
```

## Source Control Standards

To keep history consistent with the repository workflow:

- Branches must follow the domain/name-of-branch convention.
- Commit messages should use the project prefixes such as feature:, fix:, test:, and docs:.
- All changes must be reviewed through pull requests rather than direct commits to shared branches.

#### Branch naming examples

- `feature/progressive-overload-algorithm`
- `fix/workout-timer-reset`
- `test/lift-dispatch-e2e`
- `docs/coding-standards-update`

#### Commit message examples

- `feature: add progressive overload recommendation calculation endpoint`
- `fix: prevent duplicate set submission on rapid button clicks`
- `test: add unit tests for workout session boundary validation`
- `docs: update backend layering standards with domain entity examples`

## Summary

These standards ensure OptiLifts code remains understandable, consistent, and maintainable across a mixed tech stack. Contributors should treat the repository structure, linting tools, naming conventions, and test commands in this document as the default baseline for all future development.