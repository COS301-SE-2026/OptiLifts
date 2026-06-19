BEGIN;

-- ---------------------------------------------------------------------------
-- Muscles (idempotent). No unique constraint exists on muscles.name, so we
-- guard each insert with NOT EXISTS. gen_random_uuid() is built into PG13+.
-- ---------------------------------------------------------------------------
INSERT INTO muscles (muscle_id, name)
SELECT gen_random_uuid(), v.name
FROM (VALUES
    ('Abdominals'),
    ('Abductors'),
    ('Adductors'),
    ('Biceps'),
    ('Calves'),
    ('Chest'),
    ('Forearms'),
    ('Glutes'),
    ('Hamstrings'),
    ('Lats'),
    ('Lower Back'),
    ('Middle Back'),
    ('Quadriceps'),
    ('Shoulders'),
    ('Traps'),
    ('Triceps')
) AS v(name)
WHERE NOT EXISTS (SELECT 1 FROM muscles m WHERE m.name = v.name);

-- ---------------------------------------------------------------------------
-- Stable demo ids + literals.
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE seed_constants (
    exercise_bench_id uuid NOT NULL,
    exercise_squat_id uuid NOT NULL,
    exercise_pulldown_id uuid NOT NULL,
    exercise_incline_id uuid NOT NULL,
    exercise_row_id uuid NOT NULL,
    exercise_rdl_id uuid NOT NULL,
    exercise_lunge_id uuid NOT NULL,
    exercise_ohp_id uuid NOT NULL,
    exercise_calf_id uuid NOT NULL,
    folder_push_id uuid NOT NULL,
    folder_pull_id uuid NOT NULL,
    workout_push_id uuid NOT NULL,
    workout_pull_id uuid NOT NULL,
    workout_upper_b_id uuid NOT NULL,
    workout_lower_b_id uuid NOT NULL,
    we_bench_id uuid NOT NULL,
    we_squat_id uuid NOT NULL,
    we_pulldown_id uuid NOT NULL,
    we_incline_id uuid NOT NULL,
    we_row_id uuid NOT NULL,
    we_rdl_id uuid NOT NULL,
    we_lunge_id uuid NOT NULL,
    we_ohp_id uuid NOT NULL,
    we_calf_id uuid NOT NULL,
    set_bench_id uuid NOT NULL,
    set_squat_id uuid NOT NULL,
    set_pulldown_id uuid NOT NULL,
    set_incline_id uuid NOT NULL,
    set_row_id uuid NOT NULL,
    set_rdl_id uuid NOT NULL,
    set_lunge_id uuid NOT NULL,
    set_ohp_id uuid NOT NULL,
    set_calf_id uuid NOT NULL,
    test_user_email text NOT NULL,
    demo_user_email text NOT NULL,
    set_type text NOT NULL,
    exercise_type text NOT NULL,
    mechanic_compound text NOT NULL,
    equipment_barbell text NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_constants
VALUES (
    '11111111-1111-1111-1111-111111111111',
    '11111111-1111-1111-1111-111111111112',
    '11111111-1111-1111-1111-111111111113',
    '11111111-1111-1111-1111-111111111114',
    '11111111-1111-1111-1111-111111111115',
    '11111111-1111-1111-1111-111111111116',
    '11111111-1111-1111-1111-111111111117',
    '11111111-1111-1111-1111-111111111118',
    '11111111-1111-1111-1111-111111111119',
    '22222222-2222-2222-2222-222222222222',
    '22222222-2222-2222-2222-222222222223',
    '33333333-3333-3333-3333-333333333333',
    '33333333-3333-3333-3333-333333333334',
    '33333333-3333-3333-3333-333333333335',
    '33333333-3333-3333-3333-333333333336',
    '55555555-5555-5555-5555-555555555551',
    '55555555-5555-5555-5555-555555555552',
    '55555555-5555-5555-5555-555555555553',
    '55555555-5555-5555-5555-555555555554',
    '55555555-5555-5555-5555-555555555555',
    '55555555-5555-5555-5555-555555555556',
    '55555555-5555-5555-5555-555555555557',
    '55555555-5555-5555-5555-555555555558',
    '55555555-5555-5555-5555-555555555559',
    '44444444-4444-4444-4444-444444444444',
    '44444444-4444-4444-4444-444444444445',
    '44444444-4444-4444-4444-444444444446',
    '44444444-4444-4444-4444-444444444447',
    '44444444-4444-4444-4444-444444444448',
    '44444444-4444-4444-4444-444444444449',
    '44444444-4444-4444-4444-444444444450',
    '44444444-4444-4444-4444-444444444451',
    '44444444-4444-4444-4444-444444444452',
    'test@optilifts.com',
    'demo2@optilifts.com',
    'Normal',
    'WeightReps',
    'compound',
    'barbell'
);

-- ---------------------------------------------------------------------------
-- Exercise dictionary (global exercises, user_id = NULL). primary_muscle is a
-- required FK into muscles, resolved by name.
-- ---------------------------------------------------------------------------
INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_bench_id, 'Barbell Bench Press', c.mechanic_compound, c.equipment_barbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Chest'
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_incline_id, 'Incline Dumbbell Press', c.mechanic_compound, 'dumbbell', c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Chest'
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_row_id, 'Seated Cable Row', c.mechanic_compound, 'cable', c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Middle Back'
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_rdl_id, 'Romanian Deadlift', c.mechanic_compound, c.equipment_barbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Hamstrings'
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_lunge_id, 'Walking Lunge', 'complex', 'dumbbell', c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Quadriceps'
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_ohp_id, 'Overhead Press', c.mechanic_compound, c.equipment_barbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Shoulders'
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_calf_id, 'Standing Calf Raise', 'isolated', 'machine', c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Calves'
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_squat_id, 'Back Squat', 'complex', c.equipment_barbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Quadriceps'
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_pulldown_id, 'Lat Pulldown', 'isolated', 'machine', c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = 'Lats'
ON CONFLICT (exercise_dict_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Secondary muscles (sec_muscles join). Guarded by NOT EXISTS per pair.
-- ---------------------------------------------------------------------------
INSERT INTO sec_muscles (sec_muscle_id, muscle_id, exercise_id)
SELECT gen_random_uuid(), m.muscle_id, e.exercise_id
FROM (VALUES
    ('11111111-1111-1111-1111-111111111111'::uuid, 'Triceps'),
    ('11111111-1111-1111-1111-111111111111'::uuid, 'Shoulders'),
    ('11111111-1111-1111-1111-111111111114'::uuid, 'Shoulders'),
    ('11111111-1111-1111-1111-111111111114'::uuid, 'Triceps'),
    ('11111111-1111-1111-1111-111111111115'::uuid, 'Biceps'),
    ('11111111-1111-1111-1111-111111111115'::uuid, 'Shoulders'),
    ('11111111-1111-1111-1111-111111111116'::uuid, 'Glutes'),
    ('11111111-1111-1111-1111-111111111116'::uuid, 'Lower Back'),
    ('11111111-1111-1111-1111-111111111117'::uuid, 'Glutes'),
    ('11111111-1111-1111-1111-111111111117'::uuid, 'Hamstrings'),
    ('11111111-1111-1111-1111-111111111117'::uuid, 'Calves'),
    ('11111111-1111-1111-1111-111111111118'::uuid, 'Triceps'),
    ('11111111-1111-1111-1111-111111111118'::uuid, 'Chest'),
    ('11111111-1111-1111-1111-111111111112'::uuid, 'Glutes'),
    ('11111111-1111-1111-1111-111111111112'::uuid, 'Hamstrings'),
    ('11111111-1111-1111-1111-111111111113'::uuid, 'Biceps')
) AS e(exercise_id, muscle_name)
JOIN muscles m ON m.name = e.muscle_name
WHERE NOT EXISTS (
    SELECT 1 FROM sec_muscles s WHERE s.exercise_id = e.exercise_id AND s.muscle_id = m.muscle_id
);

-- ---------------------------------------------------------------------------
-- Folders.
-- ---------------------------------------------------------------------------
INSERT INTO folders (folder_id, user_id, name, description, created_at)
SELECT c.folder_push_id, u.user_id, 'Starter Push', 'Demo folder for local testing', NOW()
FROM seed_constants c
JOIN users u ON u.email_hash = encode(sha256(c.test_user_email::bytea), 'hex')
ON CONFLICT (folder_id) DO NOTHING;

INSERT INTO folders (folder_id, user_id, name, description, created_at)
SELECT c.folder_pull_id, u.user_id, 'Starter Pull', 'Demo folder for user two', NOW()
FROM seed_constants c
JOIN users u ON u.email_hash = encode(sha256(c.demo_user_email::bytea), 'hex')
ON CONFLICT (folder_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Workouts (created_by column is now user_id).
-- ---------------------------------------------------------------------------
INSERT INTO workouts (workout_id, folder_id, name, day_index, user_id, created_at)
SELECT c.workout_push_id, f.folder_id, 'Push Day A', 1, u.user_id, NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_push_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email_hash = encode(sha256(c.test_user_email::bytea), 'hex')
ON CONFLICT (workout_id) DO NOTHING;

INSERT INTO workouts (workout_id, folder_id, name, day_index, user_id, created_at)
SELECT c.workout_pull_id, f.folder_id, 'Pull Day A', 1, u.user_id, NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_pull_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email_hash = encode(sha256(c.demo_user_email::bytea), 'hex')
ON CONFLICT (workout_id) DO NOTHING;

INSERT INTO workouts (workout_id, folder_id, name, day_index, user_id, created_at)
SELECT c.workout_upper_b_id, f.folder_id, 'Upper B', 2, u.user_id, NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_push_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email_hash = encode(sha256(c.test_user_email::bytea), 'hex')
ON CONFLICT (workout_id) DO NOTHING;

INSERT INTO workouts (workout_id, folder_id, name, day_index, user_id, created_at)
SELECT c.workout_lower_b_id, f.folder_id, 'Lower B', 2, u.user_id, NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_pull_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email_hash = encode(sha256(c.demo_user_email::bytea), 'hex')
ON CONFLICT (workout_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Workout exercises (which exercise sits in which workout, ordered).
-- order_index = position of the exercise within the workout.
-- ---------------------------------------------------------------------------
INSERT INTO workout_exercises (workout_exercise_id, workout_id, exercise_dict_id, order_index)
SELECT v.we_id, v.workout_id, v.exercise_id, v.order_index
FROM seed_constants c
CROSS JOIN LATERAL (VALUES
    (c.we_bench_id,    c.workout_push_id,    c.exercise_bench_id,    1),
    (c.we_squat_id,    c.workout_push_id,    c.exercise_squat_id,    2),
    (c.we_pulldown_id, c.workout_pull_id,    c.exercise_pulldown_id, 1),
    (c.we_incline_id,  c.workout_upper_b_id, c.exercise_incline_id,  1),
    (c.we_row_id,      c.workout_upper_b_id, c.exercise_row_id,      2),
    (c.we_ohp_id,      c.workout_upper_b_id, c.exercise_ohp_id,      3),
    (c.we_rdl_id,      c.workout_lower_b_id, c.exercise_rdl_id,      1),
    (c.we_lunge_id,    c.workout_lower_b_id, c.exercise_lunge_id,    2),
    (c.we_calf_id,     c.workout_lower_b_id, c.exercise_calf_id,     3)
) AS v(we_id, workout_id, exercise_id, order_index)
ON CONFLICT (workout_exercise_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Sets (now attached to workout_exercises, not directly to workouts).
-- One set per workout-exercise here, so set order_index = 1.
-- ---------------------------------------------------------------------------
INSERT INTO sets (set_id, workout_exercise_id, set_type, reps, weight, order_index, rest_time)
SELECT v.set_id, v.we_id, c.set_type, v.reps, v.weight, 1, v.rest_time
FROM seed_constants c
CROSS JOIN LATERAL (VALUES
    (c.set_bench_id,    c.we_bench_id,    8,  60::real,   90),
    (c.set_squat_id,    c.we_squat_id,    5,  120::real,  120),
    (c.set_pulldown_id, c.we_pulldown_id, 10, 40::real,   90),
    (c.set_incline_id,  c.we_incline_id,  10, 32.5::real, 90),
    (c.set_row_id,      c.we_row_id,      12, 50::real,   75),
    (c.set_ohp_id,      c.we_ohp_id,      8,  40::real,   90),
    (c.set_rdl_id,      c.we_rdl_id,      6,  100::real,  120),
    (c.set_lunge_id,    c.we_lunge_id,    10, 24::real,   75),
    (c.set_calf_id,     c.we_calf_id,     12, 60::real,   60)
) AS v(set_id, we_id, reps, weight, rest_time)
ON CONFLICT (set_id) DO NOTHING;

COMMIT;
