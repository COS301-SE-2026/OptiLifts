BEGIN;

CREATE TEMP TABLE seed_constants (
    exercise_bench_id uuid NOT NULL,
    exercise_squat_id uuid NOT NULL,
    exercise_pulldown_id uuid NOT NULL,
    folder_push_id uuid NOT NULL,
    folder_pull_id uuid NOT NULL,
    workout_push_id uuid NOT NULL,
    workout_pull_id uuid NOT NULL,
    set_bench_id uuid NOT NULL,
    set_squat_id uuid NOT NULL,
    set_pulldown_id uuid NOT NULL,
    test_user_email text NOT NULL,
    demo_user_email text NOT NULL,
    set_type text NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_constants (
    exercise_bench_id,
    exercise_squat_id,
    exercise_pulldown_id,
    folder_push_id,
    folder_pull_id,
    workout_push_id,
    workout_pull_id,
    set_bench_id,
    set_squat_id,
    set_pulldown_id,
    test_user_email,
    demo_user_email,
    set_type
)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-111111111112',
    '11111111-1111-1111-1111-111111111113',
    '22222222-2222-2222-2222-222222222222',
    '22222222-2222-2222-2222-222222222223',
    '33333333-3333-3333-3333-333333333333',
    '33333333-3333-3333-3333-333333333334',
    '44444444-4444-4444-4444-444444444444',
    '44444444-4444-4444-4444-444444444445',
    '44444444-4444-4444-4444-444444444446',
    'test@optilifts.com',
    'demo2@optilifts.com',
    'Normal'
);

INSERT INTO exercises (
    exercise_id,
    name,
    mechanic,
    equipment,
    category,
    primary_muscles,
    secondary_muscles
)
SELECT
    c.exercise_bench_id,
    'Barbell Bench Press',
    'compound',
    'barbell',
    '',
    ARRAY['Chest']::text[],
    ARRAY['Triceps', 'Shoulders']::text[]
FROM seed_constants c
JOIN users u ON u.email = c.test_user_email
ON CONFLICT (exercise_id) DO NOTHING;

INSERT INTO exercises (
    exercise_id,
    name,
    mechanic,
    equipment,
    category,
    primary_muscles,
    secondary_muscles
)
SELECT
    c.exercise_squat_id,
    'Back Squat',
    'complex',
    'barbell',
    '',
    ARRAY['Quadriceps', 'Glutes']::text[],
    ARRAY['Hamstrings']::text[]
FROM seed_constants c
JOIN users u ON u.email = c.test_user_email
ON CONFLICT (exercise_id) DO NOTHING;

INSERT INTO exercises (
    exercise_id,
    name,
    mechanic,
    equipment,
    category,
    primary_muscles,
    secondary_muscles
)
SELECT
    c.exercise_pulldown_id,
    'Lat Pulldown',
    'isolated',
    'machine',
    '',
    ARRAY['Lats']::text[],
    ARRAY['Biceps']::text[]
FROM seed_constants c
JOIN users u ON u.email = c.test_user_email
ON CONFLICT (exercise_id) DO NOTHING;

INSERT INTO folders (
    folder_id,
    user_id,
    name,
    description,
    created_at
)
SELECT
    c.folder_push_id,
    u.user_id,
    'Starter Push',
    'Demo folder for local testing',
    NOW()
FROM seed_constants c
JOIN users u ON u.email = c.test_user_email
ON CONFLICT (folder_id) DO NOTHING;

INSERT INTO folders (
    folder_id,
    user_id,
    name,
    description,
    created_at
)
SELECT
    c.folder_pull_id,
    u.user_id,
    'Starter Pull',
    'Demo folder for user two',
    NOW()
FROM seed_constants c
JOIN users u ON u.email = c.demo_user_email
ON CONFLICT (folder_id) DO NOTHING;

INSERT INTO workouts (
    workout_id,
    folder_id,
    name,
    day_index,
    created_by,
    created_at
)
SELECT
    c.workout_push_id,
    f.folder_id,
    'Push Day A',
    1,
    u.user_id,
    NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_push_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email = c.test_user_email
ON CONFLICT (workout_id) DO NOTHING;

INSERT INTO workouts (
    workout_id,
    folder_id,
    name,
    day_index,
    created_by,
    created_at
)
SELECT
    c.workout_pull_id,
    f.folder_id,
    'Pull Day A',
    1,
    u.user_id,
    NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_pull_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email = c.demo_user_email
ON CONFLICT (workout_id) DO NOTHING;

INSERT INTO sets (
    set_id,
    workout_id,
    exercise_id,
    set_type,
    reps,
    weight,
    order_index,
    rest_time
)
SELECT
    c.set_bench_id,
    c.workout_push_id,
    c.exercise_bench_id,
    c.set_type,
    8,
    60,
    1,
    90
FROM seed_constants c
ON CONFLICT (set_id) DO NOTHING;

INSERT INTO sets (
    set_id,
    workout_id,
    exercise_id,
    set_type,
    reps,
    weight,
    order_index,
    rest_time
)
SELECT
    c.set_squat_id,
    c.workout_push_id,
    c.exercise_squat_id,
    c.set_type,
    5,
    120,
    2,
    120
FROM seed_constants c
ON CONFLICT (set_id) DO NOTHING;

INSERT INTO sets (
    set_id,
    workout_id,
    exercise_id,
    set_type,
    reps,
    weight,
    order_index,
    rest_time
)
SELECT
    c.set_pulldown_id,
    c.workout_pull_id,
    c.exercise_pulldown_id,
    c.set_type,
    10,
    40,
    1,
    90
FROM seed_constants c
ON CONFLICT (set_id) DO NOTHING;

COMMIT;