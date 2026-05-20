# OptiLifts Repository Structure & Strategy

## 1. Branching Strategy
We follow the **Git Flow** Branching Strategy
``` mermaid
%%{init: { 
  'theme': 'base', 
  'themeVariables': {
    'git0': '#900C3F', 
    'git1': '#C70039', 
    'git2': '#FF5733', 
    'git3': '#FF8D1A',
    'git4': '#FF3377',
    'commitLabelColor': '#FFEDA6',
    'commitLabelBackground': '#581845'
  } 
} }%%
gitGraph
    %% Main branch setup
    commit id: "Initial setup"
    
    %% Create integration branch
    branch dev
    checkout dev
    commit id: "baseline"
    
    %% First Feature
    branch feature/add-workout
    checkout feature/add-workout
    commit id: "create UI"
    commit id: "connect API"
    checkout dev
    merge feature/add-workout id: "PR: Add Workout"
    
    %% Bug Fix
    branch fix/rpe-calculation
    checkout fix/rpe-calculation
    commit id: "fix formula"
    checkout dev
    merge fix/rpe-calculation id: "PR: Fix RPE"
    
    %% Second Feature
    branch feature/customize-profile
    checkout feature/customize-profile
    commit id: "add avatar upload"
    checkout dev
    merge feature/customize-profile id: "PR: Profile"
    
    %% Release to Main
    checkout main
    merge dev tag: "v0.2.0"

```
---

### Branch Names
All branches must must follow the format "domain/name-of-branch"

*   `feature/name-of-feature`
*   `fix/item-fixed`
*   `hotfix/emergency-situation`
*   `test/item-tested`
*   `chore/admin-or-setup`
*   `docs/documentation-done`

Example: `fix/login-ui`
## Commits 

### Commit Messages
Commit messages must follow: 

*   `feature:` Name of feature (or components)
*   `test:` Name of test
*   `fix:` Name of fix
*   `chore:` Name/task (admin-related, like setting up boilerplate)
*   `doc:` Name of doc

Example: `feature: implement dynamic scheduling priority queue`
---

##  Pull Requests

### PR titles
PR titles should follow:

*   `Feature/title of the feature`
*   `Fix/item fixed`
*   `Hotfix/emergency situation`
*   `Test/item that has been tested`
*   `Chore/admin related task`
*   `Docs/documentationdone`

Example: `Feature/create exercise usecase`

### PR Description Template
Every PR must include a clear description of the changes made. Use the following format:

    **Description:**
    - [Bullet point 1 detailing specific technical change]
    - [Bullet point 2 detailing specific technical change]

    **Issues completed:** #[Issue_Number]


### PR Rules
*   All code must be reviewed and merged via a Pull Request. Direct commits to `dev` or `main` are not allowed.
*   PRs to dev require CI to pass as well as 2 peer reviews
*   PRs to main require CI to pass as well as 3 peer reviews

---
