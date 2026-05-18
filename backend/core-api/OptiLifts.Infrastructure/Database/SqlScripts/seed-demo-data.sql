BEGIN;

INSERT INTO exercises (
    exercise_id,
    name,
    mechanic,
    equipment,
    category,
    primary_muscles,
    secondary_muscles
)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'Barbell Bench Press',
    'compound',
    'barbell',
    '',
    ARRAY['Chest']::text[],
    ARRAY['Triceps', 'Shoulders']::text[]
)
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
VALUES (
    '11111111-1111-1111-1111-111111111112',
    'Back Squat',
    'complex',
    'barbell',
    '',
    ARRAY['Quadriceps','Glutes']::text[],
    ARRAY['Hamstrings']::text[]
)
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
VALUES (
    '11111111-1111-1111-1111-111111111113',
    'Lat Pulldown',
    'isolated',
    'machine',
    '',
    ARRAY['Lats']::text[],
    ARRAY['Biceps']::text[]
)
ON CONFLICT (exercise_id) DO NOTHING;

INSERT INTO folders (
    folder_id,
    user_id,
    name,
    description,
    created_at
)
SELECT
    '22222222-2222-2222-2222-222222222222',
    u.user_id,
    'Starter Push',
    'Demo folder for local testing',
    NOW()
FROM users u
WHERE u.email = 'test@optilifts.com'
ON CONFLICT (folder_id) DO NOTHING;

INSERT INTO folders (
    folder_id,
    user_id,
    name,
    description,
    created_at
)
SELECT
    '22222222-2222-2222-2222-222222222223',
    u.user_id,
    'Starter Pull',
    'Demo folder for user two',
    NOW()
FROM users u
WHERE u.email = 'demo2@optilifts.com'
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
        '33333333-3333-3333-3333-333333333333',
        f.folder_id,
        'Push Day A',
        1,
        u.user_id,
        NOW()
FROM folders f
JOIN users u ON u.user_id = f.user_id
WHERE u.email = 'test@optilifts.com'
    AND f.folder_id = '22222222-2222-2222-2222-222222222222'
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
        '33333333-3333-3333-3333-333333333334',
        f.folder_id,
        'Pull Day A',
        1,
        u.user_id,
        NOW()
FROM folders f
JOIN users u ON u.user_id = f.user_id
WHERE u.email = 'demo2@optilifts.com'
    AND f.folder_id = '22222222-2222-2222-2222-222222222223'
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
VALUES (
    '44444444-4444-4444-4444-444444444444',
    '33333333-3333-3333-3333-333333333333',
    '11111111-1111-1111-1111-111111111111',
    'Normal',
    8,
    60,
    1,
    90
)
ON CONFLICT (set_id) DO NOTHING;

INSERT INTO sets (set_id, workout_id, exercise_id, set_type, reps, weight, order_index, rest_time)
VALUES (
    '44444444-4444-4444-4444-444444444445', '33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111112', 'Normal', 5, 120, 2, 120
)
ON CONFLICT (set_id) DO NOTHING;

INSERT INTO sets (set_id, workout_id, exercise_id, set_type, reps, weight, order_index, rest_time)
VALUES (
    '44444444-4444-4444-4444-444444444446', '33333333-3333-3333-3333-333333333334', '11111111-1111-1111-1111-111111111113', 'Normal', 10, 40, 1, 90
)
ON CONFLICT (set_id) DO NOTHING;

COMMIT;