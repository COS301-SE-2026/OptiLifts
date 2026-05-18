<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/images/logo-animated-dark.svg">
    <img src="docs/images/logo-animated-light.svg" width="160" alt="OptiLifts" />
  </picture>
</p>

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/images/wordmark-dark.svg">
    <img src="docs/images/wordmark-light.svg" height="40" alt="OPTILIFTS">
  </picture>
</p>

<p align="center"><sub>Intelligence behind every lift.</sub></p>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

<p align="center">
  <sub>Built by</sub><br/>
  <img src="docs/images/hatrockbanner.png" height="180" width="auto" alt="hatrock" />
</p>

<div align="center">

  [![Build](https://img.shields.io/github/actions/workflow/status/COS301-SE-2026/OptiLifts/ci.yml?branch=main&style=for-the-badge&logo=githubactions&logoColor=white&label=Build&labelColor=1C1C1F&color=B01030)](https://github.com/COS301-SE-2026/OptiLifts/actions)
  [![Coverage](https://img.shields.io/badge/Coverage-Pending-D94060?style=for-the-badge&logo=codecov&logoColor=white&labelColor=1C1C1F)](https://github.com/COS301-SE-2026/OptiLifts)
  [![Issues](https://img.shields.io/github/issues/COS301-SE-2026/OptiLifts?style=for-the-badge&logo=github&logoColor=white&label=Issues&labelColor=1C1C1F&color=D94060)](https://github.com/COS301-SE-2026/OptiLifts/issues)
  [![Last Commit](https://img.shields.io/github/last-commit/COS301-SE-2026/OptiLifts?style=for-the-badge&logo=git&logoColor=white&label=Last%20Commit&labelColor=1C1C1F&color=B01030)](https://github.com/COS301-SE-2026/OptiLifts/commits/main)
  [![License](https://img.shields.io/badge/License-MIT-B01030?style=for-the-badge&labelColor=1C1C1F)](LICENSE)
  [![Uptime](https://img.shields.io/badge/Uptime-Pending-D94060?style=for-the-badge&logo=uptimerobot&logoColor=white&labelColor=1C1C1F)](https://github.com/COS301-SE-2026/OptiLifts)

</div>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/telescope.svg" width="24" height="24" style="vertical-align: middle;"> The Problem

Traditional fitness applications act as digital notebooks - they record data without ever acting on it. The burden of calculating progressive overload, recovery, and periodisation is placed entirely on the user.

<table width="100%">
  <tr>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/trending-down.svg" width="20" height="20" style="vertical-align: middle;"> Plateau Effect</h3>
      <p align="center">Without a systematic approach to progressive overload, athletes stagnate - going weeks or months without measurable improvement.</p>
    </td>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/calendar-x.svg" width="20" height="20" style="vertical-align: middle;"> Rigid Plans</h3>
      <p align="center">Pre-planned routines break down the moment life intervenes. Missed sessions leave users with no guidance on how to recover or adapt.</p>
    </td>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/brain.svg" width="20" height="20" style="vertical-align: middle;"> No Context-Awareness</h3>
      <p align="center">Apps ignore fatigue, schedule constraints, and recovery needs - forcing users to make complex athletic science decisions themselves.</p>
    </td>
  </tr>
</table>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/cpu.svg" width="24" height="24" style="vertical-align: middle;"> The Solution

OptiLifts transforms manual strength training into an intelligent, automatic experience. The system addresses the "athletic plateau" by using historical workout data to generate personalised, optimised training programs - automatically adjusting when life gets in the way.

- **Progression Engine:** Analyses your history and recommends precise weight and rep increments using XGBoost-powered plateau detection.
- **Dynamic Scheduling:** Automatically re-prioritises or reschedules missed sessions to keep muscle groups balanced and recovery on track.
- **RPE Integration:** Captures your Rate of Perceived Exertion mid-session to adapt the workout in real time, preventing injury and burnout.
- **POPIA-Compliant Privacy:** A dedicated anonymisation layer strips personal identifiers before any data reaches the AI layer.

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/layers.svg" width="24" height="24" style="vertical-align: middle;"> Tech Stack

| **Component** | **Technology** | **Description** |
| :--- | :--- | :--- |
| **Frontend** | <img src="https://skillicons.dev/icons?i=react,vite" valign="middle" /> | React SPA with React Router, built and bundled by Vite with PWA support via vite-plugin-pwa. |
| **Core API** | <img src="https://skillicons.dev/icons?i=dotnet" valign="middle" /> | ASP.NET Core with EF Core for data access and MediatR for clean CQRS-style command/query handling. |
| **AI Backend** | <img src="https://skillicons.dev/icons?i=python,fastapi" valign="middle" /> | FastAPI service using LiteLLM as a unified LLM gateway, with Langfuse for LLM observability and tracing. |
| **Machine Learning** | <img src="https://img.shields.io/badge/XGBoost-Plateau%20Detection-EC6C2D?style=flat-square&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PC9zdmc+" valign="middle" /> | XGBoost gradient-boosted model for plateau detection and progression recommendation. |
| **LLM** | <img src="https://skillicons.dev/icons?i=azure" valign="middle" /> | Azure OpenAI - GPT-4o mini for natural language coaching, workout summarisation, and intelligent suggestions. |
| **Cloud Hosting** | <img src="https://skillicons.dev/icons?i=azure" valign="middle" /> | Microsoft Azure - App Service, Container Registry, and managed PostgreSQL under Azure for Students. |
| **IaC** | <img src="https://cdn.simpleicons.org/pulumi/8A3391" width="40" valign="middle" /> | Pulumi for defining and provisioning all Azure infrastructure as code. |
| **CI/CD** | <img src="https://skillicons.dev/icons?i=githubactions" valign="middle" /> | GitHub Actions pipelines for automated testing, linting, and deployment on every push to `main`. |
| **Containerisation** | <img src="https://skillicons.dev/icons?i=docker" valign="middle" /> | Docker Compose for local development orchestration of all services. |

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/boxes.svg" width="24" height="24" style="vertical-align: middle;"> Team

| | Name | Role | GitHub | LinkedIn |
| :---: | :--- | :--- | :---: | :---: |
| <img src="docs/images/Jordan_circle.svg"/> | **Jordan Naidoo** | Team Lead (DevOps) | [![GitHub](https://skillicons.dev/icons?i=github)](https://github.com/JordanNaidoo) | [![LinkedIn](https://img.shields.io/badge/-LinkedIn-0A66C2?style=flat-square&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/jordan-naidoo/) |
| <img src="docs/images/Cailin_circle.svg"/> | **Cailin Smith** | Frontend | [![GitHub](https://skillicons.dev/icons?i=github)](https://github.com/CailinSmith) | [![LinkedIn](https://img.shields.io/badge/-LinkedIn-0A66C2?style=flat-square&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/cailin-smith-cc1307/) |
| <img src="docs/images/Alex_circle.svg"/> | **Alex Lange** | Backend | [![GitHub](https://skillicons.dev/icons?i=github)](https://github.com/AlexLange1st) | [![LinkedIn](https://img.shields.io/badge/-LinkedIn-0A66C2?style=flat-square&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/alex-lange-7444b8358/) |
| <img src="docs/images/Edwin_circle.svg"/> | **Edwin Küsel** | Backend | [![GitHub](https://skillicons.dev/icons?i=github)](https://github.com/EdwinKusel1) | [![LinkedIn](https://img.shields.io/badge/-LinkedIn-0A66C2?style=flat-square&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/edwin-kusel/) |
| <img src="docs/images/Ale_circle.svg"/> | **Alessandro Paravano** | Frontend | [![GitHub](https://skillicons.dev/icons?i=github)](https://github.com/AlessandroParavano) | [![LinkedIn](https://img.shields.io/badge/-LinkedIn-0A66C2?style=flat-square&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/aleparavano) |

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/images.svg" width="24" height="24" style="vertical-align: middle;"> Documentation

| Document | Link |
| :--- | :--- |
| Functional Requirements (SRS) | [View SRS](#) |
| Design Specification | [View Design](#) |
| GitHub Project Board | [View Board](https://github.com/orgs/COS301-SE-2026/projects) |

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/compass.svg" width="24" height="24" style="vertical-align: middle;"> Getting Started

### 1. Install Prerequisites

Make sure you have Node, pnpm, .NET 8, Python 3.12, and Docker installed.

```bash
# Node.js
sudo apt-get install -y nodejs

# npm
sudo apt-get install -y npm

# pnpm
sudo npm install -g pnpm

# .NET 8
sudo apt-get install -y dotnet-sdk-8.0

# Python 3.12
sudo apt install -y python3 python3-venv python3-pip

# Docker (WSL)
sudo apt-get install -y docker.io
```

### 2. Environment Variables

Copy the example env file and fill in your values:

```bash
cp .env.example .env
```

### 3. Setup The Repo

Run from the root directory. Installs all Node modules, restores C# packages, installs the EF Core CLI, and builds the Python virtual environment:

```bash
pnpm run setup
```

### 4. Spin up the Database

Start Docker and run:

```bash
pnpm db
pnpm db:sync
```

### 5. Seed the Database

The API seeds the demo user automatically on startup using the C# seeder. Start the backend once after the schema is ready:

```bash
pnpm dev:backend
```

Then load the rest of the demo data from SQL:

```bash
pnpm db:seed:sql
```

### 6. Start Development

```bash
pnpm dev
```

### 7. Stop Or Reset The Database

Stop containers without deleting the data:

```bash
pnpm db:down
```

Stop containers and delete the persisted volume so the records are removed:

```bash
pnpm db:down:clean
```

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/square-terminal.svg" width="24" height="24" style="vertical-align: middle;"> Commands

| Command | What it does |
| :--- | :--- |
| `pnpm run setup` | Installs Node packages, .NET packages, and the Python virtual environment |
| `pnpm build` | Builds the project |
| `pnpm db` | Starts the local PostgreSQL and Redis Docker containers |
| `pnpm db:down` | Stops the local PostgreSQL and Redis Docker containers |
| `pnpm db:down:clean` | Stops containers and removes the database volume so all records are deleted |
| `pnpm db:sync` | Pushes the initial database schema to your local container |
| `pnpm db:seed:sql` | Loads the demo workout data from the SQL script |
| `pnpm dev` | Starts the Frontend, .NET Core API, and Python AI API all at once |
| `pnpm test` | Runs all tests at once |

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/heart-handshake.svg" width="24" height="24" style="vertical-align: middle;"> Acknowledgements

- **EPI-USE** - our industry client, for the vision behind OptiLifts
- The open-source community for the incredible tools and libraries that make this possible
- Microsoft Azure for Students sponsorship

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>
