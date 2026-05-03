# OptiLifts

## Getting Started

**1. Install Prerequisites**
Make sure you have Node, pnpm, .NET 8, Python 3.12, and Docker installed.

```
//install nodejs
sudo apt-get install -y nodejs

//install pnpm
sudo npm install -g pnpm

//install .NET 8 
sudo apt-get install -y dotnet-sdk-8.0

//install Python 3.12
sudo apt install -y python3 python3-venv python3-pip

//install docker in wsl
sudo apt-get install -y docker.io
```

**2. Environment Variables**
Setup your .env file in root

**3. Setup The Repo**
Run the following command from the root directory. This will install all Node modules, restore C# packages, install the EF Core CLI, and build the Python virtual environment:
`pnpm run setup`

**4. Spin up the Database**
Start your Docker engine and run:
`pnpm db:sync` 

## Commands

| Command | What it does |
| :--- | :--- |
| `pnpm run setup` | Installs Node packages, .NET packages, and the Python virtual environment |
| `pnpm db` | Starts the local PostgreSQL and Redis Docker containers |
| `pnpm db:down` | Stops the local PostgreSQL and Redis Docker containers |
| `pnpm db:sync` | Pushes the initial database schema to your local container |
| `pnpm dev` | Starts the Frontend, .NET Core API, and Python AI API all at once |
| `pnpm test` | Runs all tests at once |