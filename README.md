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

<p align="center">
  <a href="https://readme-typing-svg.demolab.com">
    <img src="https://readme-typing-svg.demolab.com?font=Fira+Code&weight=500&size=16&duration=5000&pause=1000&color=B01030&center=true&vCenter=true&width=600&lines=Built+for+lifters%2C+by+lifters;Your+next+PR+is+already+planned" alt="Typing SVG" />
  </a>
</p>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

<p align="center">
  <sub>Built by</sub><br/>
  <img src="docs/images/hatrockbanner.png" height="180" width="auto" alt="hatrock" />
</p>

<div align="center">

<!-- CI & Status -->
[![Build](https://img.shields.io/github/actions/workflow/status/COS301-SE-2026/OptiLifts/ci.yml?branch=main&style=for-the-badge&logo=githubactions&logoColor=white&label=Build&labelColor=1C1C1F&color=B01030)](https://github.com/COS301-SE-2026/OptiLifts/actions)
[![Coverage](https://img.shields.io/badge/Coverage-Pending-D94060?style=for-the-badge&logo=codecov&logoColor=white&labelColor=1C1C1F)](https://github.com/COS301-SE-2026/OptiLifts)
[![Issues](https://img.shields.io/github/issues/COS301-SE-2026/OptiLifts?style=for-the-badge&logo=github&logoColor=white&label=Issues&labelColor=1C1C1F&color=D94060)](https://github.com/COS301-SE-2026/OptiLifts/issues)
[![Last Commit](https://img.shields.io/github/last-commit/COS301-SE-2026/OptiLifts?style=for-the-badge&logo=git&logoColor=white&label=Last%20Commit&labelColor=1C1C1F&color=B01030)](https://github.com/COS301-SE-2026/OptiLifts/commits/main)
[![License](https://img.shields.io/badge/License-MIT-B01030?style=for-the-badge&labelColor=1C1C1F)](LICENSE)
[![Uptime](https://img.shields.io/badge/Uptime-Pending-D94060?style=for-the-badge&logo=uptimerobot&logoColor=white&labelColor=1C1C1F)](https://github.com/COS301-SE-2026/OptiLifts)

</div>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/telescope.svg" width="24" height="24" style="vertical-align: middle;"> The Problem

Traditional fitness applications act as digital notebooks — they record data without ever acting on it. The burden of calculating progressive overload, recovery, and periodisation is placed entirely on the user.

<table width="100%">
  <tr>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/trending-down.svg" width="20" height="20" style="vertical-align: middle;"> Plateau Effect</h3>
      <p align="center">Without a systematic approach to progressive overload, athletes stagnate — going weeks or months without measurable improvement.</p>
    </td>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/calendar-x.svg" width="20" height="20" style="vertical-align: middle;"> Rigid Plans</h3>
      <p align="center">Pre-planned routines break down the moment life intervenes. Missed sessions leave users with no guidance on how to recover or adapt.</p>
    </td>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/brain.svg" width="20" height="20" style="vertical-align: middle;"> No Context-Awareness</h3>
      <p align="center">Apps ignore fatigue, schedule constraints, and recovery needs — forcing users to make complex athletic science decisions themselves.</p>
    </td>
  </tr>
</table>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/cpu.svg" width="24" height="24" style="vertical-align: middle;"> The Solution

OptiLifts is a workout management platform that uses AI to adapt your training before and during each session, offering intelligent suggestions to optimise your performance and ensure consistent progressive overload across every workout.

<table width="100%">
  <tr>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/trending-down.svg" width="20" height="20" style="vertical-align: middle;"> Progression Engine</h3>
      <p align="center">Analyses your history and recommends precise weight and rep increments using XGBoost-powered plateau detection.</p>
    </td>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/calendar-x.svg" width="20" height="20" style="vertical-align: middle;"> Dynamic Scheduling</h3>
      <p align="center">Automatically re-prioritises or reschedules missed sessions to keep muscle groups balanced and recovery on track.</p>
    </td>
    <td width="33%" valign="top">
      <h3 align="center"><img src="docs/images/brain.svg" width="20" height="20" style="vertical-align: middle;"> Real-Time Adaptation</h3>
      <p align="center">Captures your Rate of Perceived Exertion mid-session to adapt the workout in real time, preventing injury and burnout.</p>
    </td>
  </tr>
</table>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/layers.svg" width="24" height="24" style="vertical-align: middle;"> Architecture

```mermaid
graph LR
  A[React SPA<br/>Vite · Tailwind] -->|REST| B[ASP.NET Core API<br/>MediatR · EF Core]
  A -->|REST| C[FastAPI AI Engine<br/>XGBoost · LiteLLM]
  B -->|ORM| D[(PostgreSQL)]
  C -->|LLM Gateway| E[Azure OpenAI<br/>GPT-4o mini]
  B -->|Cache| F[(Redis)]
```

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/layers.svg" width="24" height="24" style="vertical-align: middle;"> Tech Stack

| **Component** | **Technology** | **Description** |
| :--- | :--- | :--- |
| **Frontend** | <img src="https://skillicons.dev/icons?i=react,vite" valign="middle" /> | React SPA with React Router, built and bundled by Vite with PWA support. |
| **Core API** | <img src="https://skillicons.dev/icons?i=dotnet" valign="middle" /> | ASP.NET Core with EF Core and MediatR for clean CQRS-style command/query handling. |
| **AI Backend** | <img src="https://skillicons.dev/icons?i=python,fastapi" valign="middle" /> | FastAPI service using LiteLLM as a unified LLM gateway, with Langfuse for observability. |
| **Machine Learning** | ![XGBoost](https://img.shields.io/badge/XGBoost-Plateau%20Detection-EC6C2D?style=flat-square) | XGBoost gradient-boosted model for plateau detection and progression recommendation. |
| **LLM** | <img src="https://skillicons.dev/icons?i=azure" valign="middle" /> | Azure OpenAI — GPT-4o mini for natural language coaching and intelligent suggestions. |
| **Cloud Hosting** | <img src="https://skillicons.dev/icons?i=azure" valign="middle" /> | Microsoft Azure — App Service, Container Registry, and managed PostgreSQL. |
| **IaC** | <img src="https://cdn.simpleicons.org/pulumi/8A3391" width="40" valign="middle" /> | Pulumi for defining and provisioning all Azure infrastructure as code. |
| **CI/CD** | <img src="https://skillicons.dev/icons?i=githubactions" valign="middle" /> | GitHub Actions pipelines for automated testing, linting, and deployment. |
| **Containerisation** | <img src="https://skillicons.dev/icons?i=docker" valign="middle" /> | Docker Compose for local development orchestration of all services. |

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/boxes.svg" width="24" height="24" style="vertical-align: middle;"> The Team

<p align="center">Team Hatrock — five athletes, one codebase.</p>

<br>

<!-- Row 1: Jordan + Cailin centred at same card width as row 2 -->
<table width="100%">
  <tr>
    <td width="17%"></td>
    <td width="33%" align="center" valign="top">
      <br>
      <img src="docs/images/jordan-profile.png" width="130" alt="Jordan Naidoo" />
      <br><br>
      <strong>Jordan Naidoo</strong><br>
      <img src="https://img.shields.io/badge/Team%20Lead%20·%20DevOps-B01030?style=flat-square&logoColor=white" /><br><br>
      <sub>A final-year BSc Computer Science student focused on the leadership of the OptiLifts team, spearheading DevOps and getting his bench PR up for project day.</sub>
      <br><br><br>
      <a href="https://github.com/JordanNaidoo"><img src="https://skillicons.dev/icons?i=github" width="28" /></a>&nbsp;
      <a href="https://www.linkedin.com/in/jordan-naidoo/"><img src="https://skillicons.dev/icons?i=linkedin" width="28" /></a>
      <br><br>
    </td>
    <td width="33%" align="center" valign="top">
      <br>
      <img src="docs/images/cailin-profile.png" width="130" alt="Cailin Smith" />
      <br><br>
      <strong>Cailin Smith</strong><br>
      <img src="https://img.shields.io/badge/Frontend%20Developer-B01030?style=flat-square&logoColor=white" /><br><br>
      <sub>Final-year BSc Computer Science student coordinating frontend development and design for OptiLifts. Best 1RM: 160kg calf raise.</sub>
      <br><br><br>
      <a href="https://github.com/CailinSmith"><img src="https://skillicons.dev/icons?i=github" width="28" /></a>&nbsp;
      <a href="https://www.linkedin.com/in/cailin-smith-cc1307/"><img src="https://skillicons.dev/icons?i=linkedin" width="28" /></a>
      <br><br>
    </td>
    <td width="17%"></td>
  </tr>
</table>

<!-- Row 2: Alex + Edwin + Alessandro -->
<table width="100%">
  <tr>
    <td width="33%" align="center" valign="top">
      <br>
      <img src="docs/images/alex-profile.png" width="130" alt="Alex Lange" />
      <br><br>
      <strong>Alex Lange</strong><br>
      <img src="https://img.shields.io/badge/Backend%20Developer-B01030?style=flat-square&logoColor=white" /><br><br>
      <sub>A 3rd year BSc Computer Science student with a focus on backend development for OptiLifts, and a lack of backend development in the gym.</sub>
      <br><br><br>
      <a href="https://github.com/AlexLange1st"><img src="https://skillicons.dev/icons?i=github" width="28" /></a>&nbsp;
      <a href="https://www.linkedin.com/in/alex-lange-7444b8358/"><img src="https://skillicons.dev/icons?i=linkedin" width="28" /></a>
      <br><br>
    </td>
    <td width="33%" align="center" valign="top">
      <br>
      <img src="docs/images/edwin-profile.png" width="130" alt="Edwin Küsel" />
      <br><br>
      <strong>Edwin Küsel</strong><br>
      <img src="https://img.shields.io/badge/Backend%20·%20AI%20Engineer-B01030?style=flat-square&logoColor=white" /><br><br>
      <sub>Final-year Computer Science student, focused on backend, with a passion for problem solving and hitting PRs.</sub>
      <br><br><br><br>
      <a href="https://github.com/EdwinKusel1"><img src="https://skillicons.dev/icons?i=github" width="28" /></a>&nbsp;
      <a href="https://www.linkedin.com/in/edwin-k%C3%BCsel-642b762a2/"><img src="https://skillicons.dev/icons?i=linkedin" width="28" /></a>
      <br><br>
    </td>
    <td width="33%" align="center" valign="top">
      <br>
      <img src="docs/images/ale-profile.png" width="130" alt="Alessandro Paravano" />
      <br><br>
      <strong>Alessandro Paravano</strong><br>
      <img src="https://img.shields.io/badge/Frontend%20Developer-B01030?style=flat-square&logoColor=white" /><br><br>
      <sub>Final-year BSc Computer Science student handling frontend development for OptiLifts. Skips leg day in the gym, never skips a sprint.</sub>
      <br><br><br>
      <a href="https://github.com/AlessandroParavano"><img src="https://skillicons.dev/icons?i=github" width="28" /></a>&nbsp;
      <a href="https://www.linkedin.com/in/aleparavano"><img src="https://skillicons.dev/icons?i=linkedin" width="28" /></a>
      <br><br>
    </td>
  </tr>
</table>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/images.svg" width="24" height="24" style="vertical-align: middle;"> Documentation

<div align="center">

[![Functional Requirements](https://img.shields.io/badge/Functional_Requirements-View_SRS-B01030?style=for-the-badge&logo=googledocs&logoColor=white)](#)
[![Design Specification](https://img.shields.io/badge/Design_Specification-View_Doc-B01030?style=for-the-badge&logo=googledocs&logoColor=white)](#)
[![Project Board](https://img.shields.io/badge/Project_Board-GitHub-B01030?style=for-the-badge&logo=github&logoColor=white)](https://github.com/orgs/COS301-SE-2026/projects)

</div>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/compass.svg" width="24" height="24" style="vertical-align: middle;"> Getting Started

<details open>
<summary><strong>1. Install Prerequisites</strong></summary>
<br>

Make sure you have Node, pnpm, .NET 8, Python 3.12, and Docker installed.

```bash
sudo apt-get install -y nodejs npm dotnet-sdk-8.0 python3 python3-venv python3-pip docker.io
sudo npm install -g pnpm
```

</details>

<details>
<summary><strong>2. Environment Variables</strong></summary>
<br>

```bash
cp .env.example .env
```

Fill in your values in `.env`.

</details>

<details>
<summary><strong>3. Setup the Repo</strong></summary>
<br>

Installs all Node modules, restores C# packages, installs the EF Core CLI, and builds the Python virtual environment:

```bash
pnpm run setup
```

</details>

<details>
<summary><strong>4. Start the Database</strong></summary>
<br>

```bash
pnpm db
pnpm db:sync
```

</details>

<details>
<summary><strong>5. Seed the Database</strong></summary>
<br>

The API seeds the demo user automatically on startup. Start the backend once, then load the rest of the demo data:

```bash
pnpm dev:backend
pnpm db:seed:sql
```

</details>

<details>
<summary><strong>6. Start Development</strong></summary>
<br>

```bash
pnpm dev
```

Starts the frontend, .NET Core API, and Python AI API all at once.

</details>

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/square-terminal.svg" width="24" height="24" style="vertical-align: middle;"> Commands

| Command | What it does |
| :--- | :--- |
| `pnpm run setup` | Installs Node packages, .NET packages, and the Python virtual environment |
| `pnpm build` | Builds the project |
| `pnpm db` | Starts the local PostgreSQL and Redis Docker containers |
| `pnpm db:down` | Stops the local PostgreSQL and Redis Docker containers |
| `pnpm db:down:clean` | Stops containers and removes the database volume |
| `pnpm db:sync` | Pushes the initial database schema to your local container |
| `pnpm db:seed:sql` | Loads the demo workout data from the SQL script |
| `pnpm dev` | Starts the Frontend, .NET Core API, and Python AI API all at once |
| `pnpm test` | Runs all tests at once |

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>

## <img src="docs/images/heart-handshake.svg" width="24" height="24" style="vertical-align: middle;"> Acknowledgements

- **EPI-USE** — our industry client, for the vision behind OptiLifts
- The open-source community for the incredible tools and libraries that make this possible
- Microsoft Azure for Students sponsorship

<p align="center"><img src="docs/images/divider.svg" width="800" alt="" /></p>
