BEGIN;

-- ---------------------------------------------------------------------------
-- Muscles (idempotent). No unique constraint exists on muscles.name, so the
-- insert is guarded with an anti-join (LEFT JOIN ... IS NULL).
-- gen_random_uuid() is built into PG13+.
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
LEFT JOIN muscles m ON m.name = v.name
WHERE m.muscle_id IS NULL;

-- ---------------------------------------------------------------------------
-- Stable demo ids + repeated literals, defined once (avoids duplicated
-- literals across the script). Referenced as c.<name> everywhere below.
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
    alex_user_email text NOT NULL,
    hex_enc text NOT NULL,
    set_type text NOT NULL,
    exercise_type text NOT NULL,
    mechanic_compound text NOT NULL,
    mechanic_isolated text NOT NULL,
    mechanic_complex text NOT NULL,
    equipment_barbell text NOT NULL,
    equipment_dumbbell text NOT NULL,
    equipment_cable text NOT NULL,
    equipment_machine text NOT NULL,
    equipment_bodyweight text NOT NULL,
    muscle_chest text NOT NULL,
    muscle_biceps text NOT NULL,
    muscle_triceps text NOT NULL,
    muscle_shoulders text NOT NULL,
    muscle_hamstrings text NOT NULL,
    muscle_glutes text NOT NULL,
    muscle_quadriceps text NOT NULL,
    muscle_lats text NOT NULL,
    muscle_calves text NOT NULL,
    muscle_middle_back text NOT NULL,
    muscle_lower_back text NOT NULL,
    muscle_abdominals text NOT NULL,
    badge_code_count text NOT NULL,
    badge_cat_milestone text NOT NULL
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
    'gymgoer@gmail.com',
    'hex',
    'Normal',
    'WeightReps',
    'compound',
    'isolated',
    'complex',
    'barbell',
    'dumbbell',
    'cable',
    'machine',
    'bodyweight',
    'Chest',
    'Biceps',
    'Triceps',
    'Shoulders',
    'Hamstrings',
    'Glutes',
    'Quadriceps',
    'Lats',
    'Calves',
    'Middle Back',
    'Lower Back',
    'Abdominals',
    'workout_count',
    'Milestone'
);

-- ---------------------------------------------------------------------------
-- Exercise dictionary (global exercises, user_id = NULL). primary_muscle is a
-- required FK into muscles, resolved by name.
-- ---------------------------------------------------------------------------
INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_bench_id, 'Barbell Bench Press', c.mechanic_compound, c.equipment_barbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_chest
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_incline_id, 'Incline Dumbbell Press', c.mechanic_compound, c.equipment_dumbbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_chest
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_row_id, 'Seated Cable Row', c.mechanic_compound, c.equipment_cable, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_middle_back
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_rdl_id, 'Romanian Deadlift', c.mechanic_compound, c.equipment_barbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_hamstrings
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_lunge_id, 'Walking Lunge', c.mechanic_complex, c.equipment_dumbbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_quadriceps
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_ohp_id, 'Overhead Press', c.mechanic_compound, c.equipment_barbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_shoulders
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_calf_id, 'Standing Calf Raise', c.mechanic_isolated, c.equipment_machine, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_calves
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_squat_id, 'Back Squat', c.mechanic_complex, c.equipment_barbell, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_quadriceps
ON CONFLICT (exercise_dict_id) DO NOTHING;

INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT c.exercise_pulldown_id, 'Lat Pulldown', c.mechanic_isolated, c.equipment_machine, c.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c JOIN muscles m ON m.name = c.muscle_lats
ON CONFLICT (exercise_dict_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Additional global library exercises (not referenced by the demo workouts).
-- Idempotent via an anti-join on (name, user_id IS NULL).
-- ---------------------------------------------------------------------------
WITH exercise_types AS (
    SELECT
        'BodyweightReps'::text AS bodyweight_reps,
        'WeightedBodyweight'::text AS weighted_bodyweight,
        'AssistedWeightReps'::text AS assisted_weight_reps,
        'DurationWeight'::text AS duration_weight,
        'DistanceDuration'::text AS distance_duration,
        'WeightDistance'::text AS weight_distance
)
INSERT INTO exercise_dictionary (exercise_dict_id, name, mechanic, equipment, exercise_type, primary_muscle, user_id, image_url)
SELECT gen_random_uuid(), v.name, v.mechanic, v.equipment, v.exercise_type, m.muscle_id, NULL, NULL
FROM seed_constants c
CROSS JOIN exercise_types t
CROSS JOIN LATERAL (VALUES
    ('Pull Up',             c.mechanic_compound, c.equipment_bodyweight, t.bodyweight_reps,   c.muscle_lats),
    ('Weighted Pull Up',    c.mechanic_compound, c.equipment_bodyweight, t.weighted_bodyweight, c.muscle_lats),
    ('Assisted Pull Up',    c.mechanic_compound, c.equipment_machine,    t.assisted_weight_reps, c.muscle_lats),
    ('Deadlift',            c.mechanic_compound, c.equipment_barbell,    c.exercise_type,    c.muscle_hamstrings),
    ('Dumbbell Bicep Curl', c.mechanic_isolated, c.equipment_dumbbell,   c.exercise_type,    c.muscle_biceps),
    ('Tricep Pushdown',     c.mechanic_isolated, c.equipment_cable,      c.exercise_type,    c.muscle_triceps),
    ('Plank',               c.mechanic_isolated, c.equipment_bodyweight, 'Duration',         c.muscle_abdominals),
    ('Weighted Plank',      c.mechanic_isolated, c.equipment_dumbbell,   t.duration_weight,   c.muscle_abdominals),
    ('Running',             c.mechanic_compound, c.equipment_bodyweight, t.distance_duration, c.muscle_quadriceps),
    ('Suitcase Carry',      c.mechanic_compound, c.equipment_dumbbell,   t.weight_distance,   c.muscle_lower_back)
) AS v(name, mechanic, equipment, exercise_type, muscle_name)
JOIN muscles m ON m.name = v.muscle_name
LEFT JOIN exercise_dictionary e ON e.name = v.name AND e.user_id IS NULL
WHERE e.exercise_dict_id IS NULL;

-- ---------------------------------------------------------------------------
-- Secondary muscles (sec_muscles join). Exercise ids come from seed_constants
-- (no raw UUID literals); guarded by an anti-join per pair.
-- ---------------------------------------------------------------------------
INSERT INTO sec_muscles (sec_muscle_id, muscle_id, exercise_id)
SELECT gen_random_uuid(), m.muscle_id, e.exercise_id
FROM seed_constants c
CROSS JOIN LATERAL (VALUES
    (c.exercise_bench_id,    c.muscle_triceps),
    (c.exercise_bench_id,    c.muscle_shoulders),
    (c.exercise_incline_id,  c.muscle_shoulders),
    (c.exercise_incline_id,  c.muscle_triceps),
    (c.exercise_row_id,      c.muscle_biceps),
    (c.exercise_row_id,      c.muscle_shoulders),
    (c.exercise_rdl_id,      c.muscle_glutes),
    (c.exercise_rdl_id,      c.muscle_lower_back),
    (c.exercise_lunge_id,    c.muscle_glutes),
    (c.exercise_lunge_id,    c.muscle_hamstrings),
    (c.exercise_lunge_id,    c.muscle_calves),
    (c.exercise_ohp_id,      c.muscle_triceps),
    (c.exercise_ohp_id,      c.muscle_chest),
    (c.exercise_squat_id,    c.muscle_glutes),
    (c.exercise_squat_id,    c.muscle_hamstrings),
    (c.exercise_pulldown_id, c.muscle_biceps)
) AS e(exercise_id, muscle_name)
JOIN muscles m ON m.name = e.muscle_name
LEFT JOIN sec_muscles s ON s.exercise_id = e.exercise_id AND s.muscle_id = m.muscle_id
WHERE s.sec_muscle_id IS NULL;

-- ---------------------------------------------------------------------------
-- Folders.
-- ---------------------------------------------------------------------------
INSERT INTO folders (folder_id, user_id, name, description, created_at)
SELECT c.folder_push_id, u.user_id, 'Starter Push', 'Demo folder for local testing', NOW()
FROM seed_constants c
JOIN users u ON u.email_hash = encode(sha256(c.test_user_email::bytea), c.hex_enc)
ON CONFLICT (folder_id) DO NOTHING;

INSERT INTO folders (folder_id, user_id, name, description, created_at)
SELECT c.folder_pull_id, u.user_id, 'Starter Pull', 'Demo folder for user two', NOW()
FROM seed_constants c
JOIN users u ON u.email_hash = encode(sha256(c.demo_user_email::bytea), c.hex_enc)
ON CONFLICT (folder_id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Workouts (created_by column is now user_id).
-- ---------------------------------------------------------------------------
INSERT INTO workouts (workout_id, folder_id, name, user_id, created_at)
SELECT c.workout_push_id, f.folder_id, 'Push Day A', u.user_id, NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_push_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email_hash = encode(sha256(c.test_user_email::bytea), c.hex_enc)
ON CONFLICT (workout_id) DO NOTHING;

INSERT INTO workouts (workout_id, folder_id, name, user_id, created_at)
SELECT c.workout_pull_id, f.folder_id, 'Pull Day A', u.user_id, NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_pull_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email_hash = encode(sha256(c.demo_user_email::bytea), c.hex_enc)
ON CONFLICT (workout_id) DO NOTHING;

INSERT INTO workouts (workout_id, folder_id, name, user_id, created_at)
SELECT c.workout_upper_b_id, f.folder_id, 'Upper B', u.user_id, NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_push_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email_hash = encode(sha256(c.test_user_email::bytea), c.hex_enc)
ON CONFLICT (workout_id) DO NOTHING;

INSERT INTO workouts (workout_id, folder_id, name, user_id, created_at)
SELECT c.workout_lower_b_id, f.folder_id, 'Lower B', u.user_id, NOW()
FROM seed_constants c
JOIN folders f ON f.folder_id = c.folder_pull_id
JOIN users u ON u.user_id = f.user_id
WHERE u.email_hash = encode(sha256(c.demo_user_email::bytea), c.hex_enc)
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
-- Exercise groups (superset / circuit) for demo workouts.
--   Push Day A: bench + squat        -> Superset (90s rest between)
--   Upper B:    incline + row + ohp   -> Circuit  (120s rest between)
-- Members are linked below by setting workout_exercises.group_id.
-- (Group rest lives on the group; per-set rest stays on the sets.)
-- ---------------------------------------------------------------------------
INSERT INTO exercise_groups (exercise_group_id, workout_id, group_type, rest_time)
SELECT v.group_id, v.workout_id, v.group_type, v.rest_time
FROM seed_constants c
CROSS JOIN LATERAL (VALUES
    ('66666666-6666-6666-6666-666666666661'::uuid, c.workout_push_id,    'Superset', 90),
    ('66666666-6666-6666-6666-666666666662'::uuid, c.workout_upper_b_id, 'Circuit',  120)
) AS v(group_id, workout_id, group_type, rest_time)
ON CONFLICT (exercise_group_id) DO NOTHING;

-- Link each exercise to its workout's group. Push Day A and Upper B are fully
-- grouped (all their exercises belong to the superset/circuit); Pull/Lower have
-- no group row, so their exercises stay ungrouped. Deriving group_id from the
-- workout avoids repeating the group ids.
UPDATE workout_exercises we
SET group_id = eg.exercise_group_id
FROM exercise_groups eg
WHERE eg.workout_id = we.workout_id;

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

-- ===========================================================================
-- Profile-page demo data for "Alex" (gymgoer@gmail.com).
-- The user itself is created by the C# seeder (encrypted), so run `dotnet run`
-- before this script. Logs attach to the user via scheduled_entries; the
-- encrypted workout_logs.notes column is left NULL (raw SQL can't encrypt).
-- ===========================================================================
DO $$
DECLARE
    alex_email text;
    hex_enc constant text := 'hex';
    pull_name constant text := 'Pull';
    push_name constant text := 'Push';
    full_body_name constant text := 'Full Body';
    my_split_name constant text := 'My Split';
    completed_status constant text := 'Completed';
    alex_id uuid;
    v_folder uuid;
    v_pull uuid;
    v_push uuid;
    v_full_body uuid;
    v_we uuid;
    v_ex uuid;
    v_entry uuid;
    v_log uuid;
    v_day timestamp;
    i int;
    rec record;
BEGIN
    SELECT c.alex_user_email INTO alex_email
    FROM seed_constants c
    LIMIT 1;

    SELECT user_id INTO alex_id FROM users
    WHERE email_hash = encode(sha256(alex_email::bytea), hex_enc);

    IF alex_id IS NULL THEN
        RAISE NOTICE 'Alex (%) not found - run the C# seeder (dotnet run) before this script.', alex_email;
        RETURN;
    END IF;

    DELETE FROM workout_log_sets
    WHERE log_id IN (
        SELECT log_id
        FROM workout_logs
        WHERE entry_id IN (
            SELECT entry_id
            FROM scheduled_entries
            WHERE user_id = alex_id
        )
    );

    DELETE FROM workout_logs
    WHERE entry_id IN (
        SELECT entry_id
        FROM scheduled_entries
        WHERE user_id = alex_id
    );

    DELETE FROM scheduled_entries
    WHERE user_id = alex_id;

    DELETE FROM sets
    WHERE workout_exercise_id IN (
        SELECT workout_exercise_id
        FROM workout_exercises
        WHERE workout_id IN (
            SELECT workout_id
            FROM workouts
            WHERE user_id = alex_id AND name IN (pull_name, push_name)
        )
    );

    DELETE FROM workout_exercises
    WHERE workout_id IN (
        SELECT workout_id
        FROM workouts
        WHERE user_id = alex_id AND name IN (pull_name, push_name, full_body_name)
    );

    DELETE FROM workouts
    WHERE user_id = alex_id AND name = full_body_name;

    SELECT folder_id INTO v_folder
    FROM folders
    WHERE user_id = alex_id AND name = my_split_name
    LIMIT 1;

    IF v_folder IS NULL THEN
        INSERT INTO folders (folder_id, user_id, name, description, created_at)
        VALUES (gen_random_uuid(), alex_id, my_split_name, 'Demo training split', NOW())
        RETURNING folder_id INTO v_folder;
    END IF;

    SELECT workout_id INTO v_pull
    FROM workouts
    WHERE user_id = alex_id AND name = pull_name
    LIMIT 1;

    IF v_pull IS NULL THEN
        INSERT INTO workouts (workout_id, folder_id, name, user_id, created_at)
        VALUES (gen_random_uuid(), v_folder, pull_name, alex_id, NOW())
        RETURNING workout_id INTO v_pull;
    END IF;

    SELECT workout_id INTO v_push
    FROM workouts
    WHERE user_id = alex_id AND name = push_name
    LIMIT 1;

    IF v_push IS NULL THEN
        INSERT INTO workouts (workout_id, folder_id, name, user_id, created_at)
        VALUES (gen_random_uuid(), v_folder, push_name, alex_id, NOW())
        RETURNING workout_id INTO v_push;
    END IF;

    SELECT workout_id INTO v_full_body
    FROM workouts
    WHERE user_id = alex_id AND name = full_body_name
    LIMIT 1;

    IF v_full_body IS NULL THEN
        INSERT INTO workouts (workout_id, folder_id, name, user_id, created_at)
        VALUES (gen_random_uuid(), v_folder, full_body_name, alex_id, NOW())
        RETURNING workout_id INTO v_full_body;
    END IF;

    -- exercises + sets for each recent-workout card (volume = weight*reps, count = #sets)
    FOR rec IN
        SELECT * FROM (VALUES
            (v_pull, 'Lat Pulldown',            1, 5, 12, 45::real),
            (v_pull, 'Seated Cable Row',        2, 4, 10, 50::real),
            (v_pull, 'Pull Up',                 3, 4, 8,   0::real),
            (v_pull, 'Dumbbell Bicep Curl',     4, 3, 12, 14::real),
            (v_push, 'Barbell Bench Press',     1, 4, 8,  60::real),
            (v_push, 'Overhead Press',          2, 4, 8,  40::real),
            (v_push, 'Incline Dumbbell Press',   3, 4, 10, 30::real),
            (v_push, 'Tricep Pushdown',          4, 4, 12, 25::real)
        ) AS t(workout_id, ex_name, ord, n_sets, reps, weight)
    LOOP
        SELECT exercise_dict_id INTO v_ex FROM exercise_dictionary
        WHERE name = rec.ex_name AND user_id IS NULL
        LIMIT 1;
        CONTINUE WHEN v_ex IS NULL;

        INSERT INTO workout_exercises (workout_exercise_id, workout_id, exercise_dict_id, order_index)
        VALUES (gen_random_uuid(), rec.workout_id, v_ex, rec.ord)
        RETURNING workout_exercise_id INTO v_we;

        INSERT INTO sets (set_id, workout_exercise_id, set_type, reps, weight, duration, distance, order_index, rest_time)
        SELECT gen_random_uuid(), v_we, 'Normal', rec.reps, rec.weight, NULL, NULL, gs, 90
        FROM generate_series(1, rec.n_sets) AS gs;
    END LOOP;

    FOR rec IN
        SELECT * FROM (VALUES
            (v_full_body, 'Barbell Bench Press',  1, 1,    8,   NULL::integer, 60::real,   NULL::real),
            (v_full_body, 'Pull Up',              2, 1,   10,   NULL::integer, NULL::real,  0::real),
            (v_full_body, 'Weighted Pull Up',     3, 1,    6,   NULL::integer, 10::real,    NULL::real),
            (v_full_body, 'Assisted Pull Up',     4, 1,    8,   NULL::integer, 20::real,    NULL::real),
            (v_full_body, 'Plank',                5, 1, NULL::integer, 60,     NULL::real,  NULL::real),
            (v_full_body, 'Weighted Plank',       6, 1, NULL::integer, 45,     12::real,    NULL::real),
            (v_full_body, 'Running',              7, 1, 1800,   900,          NULL::real,   5::real),
            (v_full_body, 'Suitcase Carry',       8, 1,   30,    NULL::integer, 40::real,    30::real)
        ) AS t(workout_id, ex_name, ord, n_sets, reps, duration, weight, distance)
    LOOP
        SELECT exercise_dict_id INTO v_ex FROM exercise_dictionary
        WHERE name = rec.ex_name AND user_id IS NULL
        LIMIT 1;
        CONTINUE WHEN v_ex IS NULL;

        INSERT INTO workout_exercises (workout_exercise_id, workout_id, exercise_dict_id, order_index)
        VALUES (gen_random_uuid(), rec.workout_id, v_ex, rec.ord)
        RETURNING workout_exercise_id INTO v_we;

        INSERT INTO sets (set_id, workout_exercise_id, set_type, reps, weight, duration, distance, order_index, rest_time)
        SELECT gen_random_uuid(), v_we, 'Normal', rec.reps, rec.weight, rec.duration, rec.distance, gs, 90
        FROM generate_series(1, rec.n_sets) AS gs;
    END LOOP;

    -- ~51 logged sessions spread across 2026-03-23 .. mid-June (calendar / hours / streak / count)
    FOR i IN 0..50 LOOP
        v_day := TIMESTAMP '2026-03-23 17:00:00' + (i * INTERVAL '41 hours');

        INSERT INTO scheduled_entries (entry_id, user_id, workout_id, scheduled, status)
        VALUES (gen_random_uuid(), alex_id, CASE WHEN i % 2 = 0 THEN v_push ELSE v_pull END, v_day, completed_status)
        RETURNING entry_id INTO v_entry;

        INSERT INTO workout_logs (log_id, entry_id, started_at, completed_at, ai_modified, notes)
        VALUES (gen_random_uuid(), v_entry, v_day, v_day + INTERVAL '65 minutes', false, NULL)
        RETURNING log_id INTO v_log;

        PERFORM 1
        FROM workout_log_sets
        WHERE log_id = v_log;

        IF FOUND THEN
            CONTINUE;
        END IF;

        INSERT INTO workout_log_sets (
            log_set_id, log_id, exercise_id, set_id, set_type, reps, weight, rpe, order_index, ai_suggested, logged_at)
        SELECT
            gen_random_uuid(),
            v_log,
            we.exercise_dict_id,
            s.set_id,
            s.set_type,
            s.reps,
            s.weight,
            CASE WHEN i % 3 = 0 THEN 7.5 WHEN i % 3 = 1 THEN 8.0 ELSE 8.5 END,
            s.order_index,
            false,
            v_day + (we.order_index * INTERVAL '4 minutes') + (s.order_index * INTERVAL '45 seconds')
        FROM workout_exercises we
        JOIN sets s ON s.workout_exercise_id = we.workout_exercise_id
        WHERE we.workout_id = CASE WHEN i % 2 = 0 THEN v_push ELSE v_pull END
          AND we.order_index <= 4;
    END LOOP;

    v_day := TIMESTAMP '2026-03-23 17:00:00' + (51 * INTERVAL '41 hours');

    INSERT INTO scheduled_entries (entry_id, user_id, workout_id, scheduled, status)
    VALUES (gen_random_uuid(), alex_id, v_full_body, v_day, completed_status)
    RETURNING entry_id INTO v_entry;

    INSERT INTO workout_logs (log_id, entry_id, started_at, completed_at, ai_modified, notes)
    VALUES (gen_random_uuid(), v_entry, v_day, v_day + INTERVAL '82 minutes', false, NULL)
    RETURNING log_id INTO v_log;

    INSERT INTO workout_log_sets (
        log_set_id, log_id, exercise_id, set_id, set_type, reps, weight, rpe, order_index, ai_suggested, logged_at)
    SELECT
        gen_random_uuid(),
        v_log,
        we.exercise_dict_id,
        s.set_id,
        s.set_type,
        COALESCE(s.reps, s.duration, ROUND(s.distance)::int, 1),
        COALESCE(s.weight, 0),
        CASE WHEN we.order_index % 3 = 0 THEN 7.5 WHEN we.order_index % 3 = 1 THEN 8.0 ELSE 8.5 END,
        s.order_index,
        false,
        v_day + (we.order_index * INTERVAL '4 minutes') + (s.order_index * INTERVAL '45 seconds')
    FROM workout_exercises we
    JOIN sets s ON s.workout_exercise_id = we.workout_exercise_id
    WHERE we.workout_id = v_full_body;
END $$;

-- ===========================================================================
-- Alex's upcoming schedule. Reuses Alex's own workouts and stays idempotent
-- per (user, workout, scheduled, status) row.
-- ===========================================================================
DO $$
DECLARE
    alex_email text;
    hex_enc constant text := 'hex';
    scheduled_status constant text := 'Scheduled';
    alex_id uuid;
    v_pull uuid;
    v_push uuid;
    rec record;
BEGIN
    SELECT c.alex_user_email INTO alex_email
    FROM seed_constants c
    LIMIT 1;

    SELECT user_id INTO alex_id
    FROM users
    WHERE email_hash = encode(sha256(alex_email::bytea), hex_enc);

    IF alex_id IS NULL THEN
        RAISE NOTICE 'Alex (%) not found - run the C# seeder (dotnet run) before this block.', alex_email;
        RETURN;
    END IF;

    SELECT workout_id INTO v_pull
    FROM workouts
    WHERE user_id = alex_id AND name = pull_name
    LIMIT 1;

    SELECT workout_id INTO v_push
    FROM workouts
    WHERE user_id = alex_id AND name = push_name
    LIMIT 1;

    IF v_pull IS NULL OR v_push IS NULL THEN
        RAISE NOTICE 'Alex split workouts not found - run the demo split seeding before this block.';
        RETURN;
    END IF;

    FOR rec IN
        SELECT * FROM (VALUES
            (TIMESTAMPTZ '2026-07-01 18:00:00+00', v_push),
            (TIMESTAMPTZ '2026-07-03 18:00:00+00', v_pull),
            (TIMESTAMPTZ '2026-07-05 10:00:00+00', v_push)
        ) AS t(scheduled_at, workout_id)
    LOOP
        INSERT INTO scheduled_entries (entry_id, user_id, workout_id, scheduled, status)
        SELECT gen_random_uuid(), alex_id, rec.workout_id, rec.scheduled_at, scheduled_status
        WHERE NOT EXISTS (
            SELECT 1
            FROM scheduled_entries se
            WHERE se.user_id = alex_id
              AND se.workout_id = rec.workout_id
              AND se.scheduled = rec.scheduled_at
              AND se.status = scheduled_status
        );
    END LOOP;
END $$;

-- ===========================================================================
-- Badge definitions. `code` maps to an IBadgeRule (only "workout_count" has a
-- rule today); "streak_weeks" has no rule yet but can still be awarded manually.
-- Idempotent via the unique index on badges.name.
-- ===========================================================================
INSERT INTO badges (badge_id, code, name, description, category, threshold, created_at)
SELECT gen_random_uuid(), v.code, v.name, v.description, v.category, v.threshold, NOW()
FROM seed_constants c
CROSS JOIN LATERAL (VALUES
    (c.badge_code_count, 'First Workout', 'Complete your first workout', c.badge_cat_milestone, 1),
    (c.badge_code_count, '10 Workouts',   'Complete 10 workouts',        c.badge_cat_milestone, 10),
    (c.badge_code_count, '50 Workouts',   'Complete 50 workouts',        c.badge_cat_milestone, 50),
    (c.badge_code_count, 'Century Club',  'Complete 100 workouts',       c.badge_cat_milestone, 100),
    ('streak_weeks',     'Consistent',    'Train 5 weeks in a row',      'Streak',              5)
) AS v(code, name, description, category, threshold)
ON CONFLICT (name) DO NOTHING;

-- ===========================================================================
-- Award earned badges to Alex (gymgoer@gmail.com). He has 51 workouts, so he
-- earns the three workout-count milestones + the streak badge; "Century Club"
-- (100) is intentionally left unearned. Idempotent via unique (user_id, badge_id).
-- ===========================================================================
INSERT INTO user_badges (user_badge_id, user_id, badge_id, earned_at)
SELECT gen_random_uuid(), u.user_id, b.badge_id, NOW()
FROM seed_constants c
JOIN users u ON u.email_hash = encode(sha256(c.alex_user_email::bytea), c.hex_enc)
JOIN badges b ON b.name IN ('First Workout', '10 Workouts', '50 Workouts', 'Consistent')
ON CONFLICT (user_id, badge_id) DO NOTHING;

COMMIT;
