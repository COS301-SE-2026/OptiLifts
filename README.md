# OptiLifts

## Commands

| Command | What it does |
| :--- | :--- |
| `pnpm setup` | Installs Node packages, .NET packages, and the Python virtual environment |
| `pnpm dev:db` | Starts the local PostgreSQL and Redis Docker containers |
| `pnpm db:down` | Stops the local PostgreSQL and Redis Docker containers |
| `pnpm db:sync` | Pushes the initial database schema to your local container |
| `pnpm dev` | Starts the Frontend, .NET Core API, and Python AI API all at once |
| `pnpm test` | Runs all tests at once |