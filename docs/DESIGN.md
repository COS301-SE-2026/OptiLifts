# Design Specification: OptiLifts

> For the full brand style guide see [docs/brand-style/brand-style-webpage.pdf](brand-style/brand-style-webpage.pdf)

---

## Theme Toggle

Theme state is managed via a `ThemeProvider` wrapping the app. shadcn's built-in dark mode support uses the `dark:` Tailwind variant. Theme tokens are defined as CSS custom properties in `globals.css` under `:root` (light) and `.dark` (dark), mapped to `tailwind.config.ts` via `theme.extend.colors`. The toggle uses shadcn's Switch or DropdownMenu component.

---

## Wireframes

The following wireframes represent the key screens for Demo 1. All screens are mid-fidelity - layout and component placement are finalised; final visual polish is applied in the live implementation.

---

### Navigation Flow

The navigation flow below reflects the actual routes defined in `App.tsx` and the nav links in `navbar.tsx`.

```mermaid
flowchart TD
    A["/ - Public Landing"]
    B["/register - Register"]
    C["/login - Login"]
    N["/brand-style - Brand Style"]
    J["Unauthenticated access to protected route"]
    GUARD(["RequireAuth Guard"])
    D["/dashboard - Dashboard"]
    E["/workouts - Workouts List"]
    F["/workouts/create - Create Workout"]
    K["/workouts/:workoutId - Workout Detail"]
    L["/workouts/:workoutId/logs/:logId - Workout Log Detail"]
    G["/schedule - Schedule"]
    H["/progress"]
    I["/profile - Profile"]
    M["/past-workouts - Past Workouts"]

    A --> B
    A --> N
    B --> C
    C --> B
    B -->|"success"| GUARD
    C -->|"success"| GUARD
    J -->|"redirect"| B

    GUARD --> D
    D --> E
    D --> G
    D --> H
    D --> I
    D --> M
    E -->|"Click Workout"| K
    E -->|"+ Create Workout"| F
    K -->|"View Log"| L
    F -.->|"save"| E
    I -->|"logout"| A

    classDef protected fill:#26262B,stroke:#B01030,color:#E8E8EC
    classDef public fill:#1C1C1F,stroke:#9A9AA8,color:#E8E8EC
    classDef guard fill:#B01030,stroke:#B01030,color:#FFFFFF

    class D,E,F,K,L,G,H,I,M protected
    class A,B,C,J,N public
    class GUARD guard
```

**Auth behaviour:**
- Unauthenticated users see Register and Login in the navbar
- Authenticated users see Dashboard, Workouts, Schedule, Progress, Profile and a Logout button
- Any direct navigation to a protected route while unauthenticated redirects to `/register`, preserving the intended destination in `location.state.from`
- Logout clears the session and returns the user to the public nav state

---

### Screen Layouts

#### Screen 1 - Register

Primary registration screen. Allows a new user to create an account.

![Register Wireframe](wireframes/register-wireframe.png)

**Component Placement:**
- Header: logo left, LOGIN and REGISTER nav links right
- Body: centered card containing the registration form
- Form fields stacked vertically: Username, Email Address, Password, Re-enter Password
- REGISTER primary button below fields
- "Already have an account? Login" link below button

**User Interaction Points:**
- Username field - text input, validates on blur
- Email Address field - text input, validates format on blur
- Password field - text input with show/hide toggle
- Re-enter Password field - text input with show/hide toggle, validates match
- REGISTER button - submits form, disabled until all fields valid
- Login link - navigates to Login screen

**Annotations:**
- Password fields use eye icon toggle (Lucide `Eye` / `EyeOff`)
- Inline error appears directly below the relevant field on blur
- REGISTER button activates only when all fields pass validation
- On success: user is redirected to Dashboard

---

#### Screen 2 - Login

Allows an existing user to authenticate.

![Login Wireframe](wireframes/login-wireframe.png)

**Component Placement:**
- Header: logo left, LOGIN (active, underlined) and REGISTER nav links right
- Body: centered card containing the login form
- Form fields stacked vertically: Username, Password
- Forgot Password link right-aligned below password field
- LOGIN primary button below fields
- "Don't have an account? Register" link below button

**User Interaction Points:**
- Username field - text input
- Password field - text input with show/hide toggle
- Forgot Password link - initiates password reset flow
- LOGIN button - submits credentials
- Register link - navigates to Register screen

**Annotations:**
- Active nav link (LOGIN) shows 2px bottom border in accent colour
- Inline error displayed below fields on failed authentication attempt
- On success: user is redirected to Dashboard

---

#### Screen 3 - Create Workout

Allows the athlete to build a named workout by adding exercises with sets, reps, and weight.

![Create Workout Wireframe](wireframes/create-workout-wireframe.png)

**Component Placement:**
- Header: logo left, full navigation (DASHBOARD, WORKOUTS active, SCHEDULE, PROGRESS, PROFILE) right
- Left panel (70% width): workout name input + SAVE WORKOUT button at top, exercise cards below, each with set rows
- Right panel (30% width): muscle diagram at top, Recommended exercises section, Exercise library with filters and search below
- Each exercise card: exercise name + muscle group header, set rows with SET type / KG / REPS columns, "+ Add Set" at bottom
- Right panel exercise items: exercise name + muscle label + "+" add button

**User Interaction Points:**
- Workout Name field - text input, required for save to activate
- SAVE WORKOUT button - disabled until name + at least one exercise present
- Exercise card "..." menu - edit or remove exercise
- Set row fields - inline editable KG and REPS inputs
- Set row "x" button - removes that set row
- Set type dropdown - select set type (W = working, warmup, etc.)
- "+ Add Set" - appends a new set row to the exercise card
- Right panel "+" button - adds exercise to workout
- "+ Create Exercise" link - opens create exercise flow
- Muscle filter dropdown - filters exercise list by muscle group
- Equipment filter dropdown - filters exercise list by equipment
- Search field - searches exercise library by name

**Annotations:**
- Muscle diagram updates to highlight muscles targeted by added exercises
- Recommended section shows AI-suggested exercises based on current workout composition
- Save is disabled until workout has a name and at least one exercise
- Set rows are drag-reorderable within an exercise card
- Exercise cards are reorderable within the workout

---

#### Screen 4 - My Workouts

Shows the athlete's saved workouts as a scrollable list of cards.

![My Workouts Wireframe](wireframes/my-workouts-wireframe.png)

**Component Placement:**
- Header: logo left, full navigation (DASHBOARD, WORKOUTS active, SCHEDULE, PROGRESS, PROFILE) right
- Page title "WORKOUTS" left-aligned with "+" icon button right-aligned
- Workout cards in a scrollable list, each spanning full width of the left panel
- Muscle diagram right panel showing combined muscle coverage
- Each card: workout name as heading, Primary Muscle Groups label + values, Exercises label + preview list

**User Interaction Points:**
- "+" button in header - navigates to Create Workout
- Workout card click - opens workout detail / edit view
- "..." menu on each card - reveals edit and delete options

**Annotations:**
- Muscle diagram highlights aggregate muscle groups across all visible workouts
- Exercise preview shows first 3 exercises then "..." to indicate more
- Cards use the standard card component styling (border, surface background, 22px padding)
- Delete option in "..." menu requires confirmation before removing the workout

---

#### Screen 5 - Dashboard

Shows a quick overiew of how the athlete is doing this week, what workouts are coming up, and recent milestones.

![Dashboard Wireframe](wireframes/dashboard-wireframe.png)

**Component Placement:**
- Header: logo on the left, full navigation (DASHBOARD, WORKOUTS active, SCHEDULE, PROGRESS, PROFILE) right
- Upper section: left-aligned greeting header with a subtitle of today's scheduled workout. Below are two utility buttons, "VIEW WORKOUT" and "START SESSION".
- Mid left panel: 70% width, a large card displaying a line graph tracking the user's volume for the current week. Top right of the card contains dropdown menus for muscle group filtering and week/month duration.
- Mid right panel: an "Upcoming" sidebar card listing the scheduled workouts with exercise counts and days. A dashed "See all" action button is at the bottom.
- Bottom row: grid layout, four grid blocks:
    - "Favourite exercise": displays name and icon placeholder
    - "Days exercised this week": numerical value and fire icon
    - "Personal records hit thia week": numerical value and medal achievement icon
    - Interactive radar chart showing relative muscle group balance across key sections

**User Interaction Points:**
- VIEW WORKOUT / START SESSIOON buttons - navigates to the workout active session or the details page
- Volume chart filters - dropdown selection that modifies chart data arrays dynamically
- Upcoming sidebar items - clicking specific workout navigates straight to that specific scheduled workout
- "See all" button - redirects to schedule page

**Annotations:**
- The radar chart updates to visually captrue the weekly load balancing based on the user's completed workouts volume logs

---

#### Screen 6 - Edit Workout

Allows athlete to modify an existing workout by editing the names, set ranges, adding new exercises, etc.

![Edit Workout Wireframe](wireframes/edit-workout-wireframe.png)

**Component Placement:**
- Header: logo left, full navigation (WORKOUTS active) right
- Left panel (70% width): workout name input + SAVE WORKOUT button at top, exercise cards below, each with set rows
- Right panel (30% width): muscle diagram at top, Recommended exercises section, Exercise library with filters and search below
- Each exercise card: exercise name + muscle group header, set rows with SET type / KG / REPS columns, "+ Add Set" at bottom
- Right panel exercise items: exercise name + muscle label + "+" add button

**User Interaction Points:**
- Workout Name field - text input
- SAVE WORKOUT button - update the existing workout with new edits
- Exercise card "..." menu - edit or remove exercise
- Set row fields - inline editable KG and REPS inputs
- Set row "x" button - removes that set row
- Set type dropdown - select set type (W = working, warmup, etc.)
- "+ Add Set" - appends a new set row to the exercise card
- Right panel "+" button - adds exercise to workout
- "+ Create Exercise" link - opens create exercise flow
- Muscle filter dropdown - filters exercise list by muscle group
- Equipment filter dropdown - filters exercise list by equipment
- Search field - searches exercise library by name

**Annotations:**
- Muscle diagram updates to highlight muscles targeted by added exercises
- Recommended section shows AI-suggested exercises based on current workout composition
- Set rows are drag-reorderable within an exercise card
- Exercise cards are reorderable within the workout

---
#### Screen 7 - Workout Detail

Displays the  details of a specific created workout, including set information and muscle distribution

![Workout detail Wireframe](wireframes/workout-detail-wireframe.png)

**Component Placement:**
- Header: logo left, full navigation (WORKOUTS active) right
- Page title of workout name "PULL" left-aligned with right aligned summary details
- Left column panel: Vertical scroll view displaying exercise cards, each with a exercise image, title and set and rep details
- Right column panel: muscle heatmap displaying highlight values across specific muscle groups, and a horizontal bar chart displaying the exact set count per muscle category

**User Interaction Points:**
- Card stack container - standard scrolling mechanics

**Annotations:**
- Heatmap shows a clean muscle distribution for easy viewing of the primary muscle groups a workout exercises

---
#### Screen 8 - Workout Log Detail

Displays the details of a completed workout session, including the exercises, set and rep counts, and muscle distribution

![Workout Log Detail Wireframe](wireframes/workout-log-detail-wireframe.png)

**Component Placement:**
- Header: logo left, full navigation (WORKOUTS active) right
- Page title of workout name "PULL" left-aligned with right aligned summary details
- Left container card: completed exercise cards with columns for sets, showing their types and details
- Right container card: muscle heatmap displaying highlight values across specific muscle groups, and a horizontal bar chart displaying the exact set count per muscle category

**User Interaction Points:**
- Card stack container - standard scrolling mechanics

**Annotations:**
- Heatmap shows a clean muscle distribution for easy viewing of the primary muscle groups a workout exercises
- RPE fields are shown next to the rep count

---
#### Screen 9 - Week Schedule

Calendar grid showing scheduled workout routines across a weekly context view, and displaying weekly summaries

![Week Schedule Wireframe](wireframes/week-schedule-wireframe.png)

**Component Placement:**
- Header: logo left, full navigaion (SCHEDULE active) right
- Page title "SCHEDULE" left-aligned with right date selector block with chevron toggles, and a dropdown menu for toggling Week/Month view
- Left column view: Vertical cards for each day of the week, with empty slots with centered "+" icon button, and active slots with the workout name, primary muscle groups, and workout summary and scheduled status
- Right column view: forecast panels with stats for that week, and a spider graph mapping muscle distribution over 6 main muscle groups

**User Interaction Points:**
- Chevron arrows - shifts data range backwards or forwards by a week
- Dropdown select - switches layout mode between Month and Week views
- Card "+" buttons - opens popup with created workouts to schedule them on specific days
- "X" buttons - removes the scheduled entry from the calendar

**Annotations:**
- - The radar chart updates to visually captrue the weekly load balancing based on the user's workouts set count

---
#### Screen 10 - Month Schedule

Calendar grid showing scheduled workout routines across a monthly context view

![Month Schedule Wireframe](wireframes/month-schedule-wireframe.png)

**Component Placement:**
- Header: logo left, full navigaion (SCHEDULE active) right
- Page title "SCHEDULE" left-aligned with right date selector block with chevron toggles, and a dropdown menu for toggling Week/Month view
- Center section: a structures calendar grid mapping columns MON through to SUN.

**User Interaction Points:**
- a

**Annotations:**
- a

---
#### Screen 11 - Past Workouts

description

![Past Workouts Wireframe](wireframes/past-workouts-wireframe.png)

**Component Placement:**
- Header: 

**User Interaction Points:**
- a

**Annotations:**
- a

---
#### Screen 12 - Profile

description

![Profile Wireframe](wireframes/profile-wireframe.png)

**Component Placement:**
- Header: 

**User Interaction Points:**
- a

**Annotations:**
- a

---