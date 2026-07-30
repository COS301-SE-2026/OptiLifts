BEGIN;



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
    exercise_bench_press_name text NOT NULL,
    exercise_pull_up_name text NOT NULL,
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
    rpe_exercise text NOT NULL,
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
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Barbell Bench Press' LIMIT 1),
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Barbell Back Squat' LIMIT 1),
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Cable lat pulldown%' LIMIT 1),
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Dumbbell incline bench press' LIMIT 1),
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Cable seated row' LIMIT 1),
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Barbell romanian deadlift' LIMIT 1),
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Walking lunge' LIMIT 1),
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Barbell seated overhead press' LIMIT 1),
    (SELECT exercise_dict_id FROM exercise_dictionary WHERE name ILIKE 'Standing calf raise' LIMIT 1),
    'Barbell bench press',
    'Pull-up',
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
    'exercise',
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
JOIN workouts w ON w.workout_id = v.workout_id
ON CONFLICT (workout_exercise_id) DO NOTHING;

INSERT INTO exercise_groups (exercise_group_id, workout_id, group_type, rest_time)
SELECT v.group_id, v.workout_id, v.group_type, v.rest_time
FROM seed_constants c
CROSS JOIN LATERAL (VALUES
    ('66666666-6666-6666-6666-666666666661'::uuid, c.workout_push_id,    'Superset', 90),
    ('66666666-6666-6666-6666-666666666662'::uuid, c.workout_upper_b_id, 'Circuit',  120)
) AS v(group_id, workout_id, group_type, rest_time)
JOIN workouts w ON w.workout_id = v.workout_id
ON CONFLICT (exercise_group_id) DO NOTHING;

UPDATE workout_exercises we
SET group_id = eg.exercise_group_id
FROM exercise_groups eg
WHERE eg.workout_id = we.workout_id;

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
JOIN workout_exercises we ON we.workout_exercise_id = v.we_id
ON CONFLICT (set_id) DO NOTHING;

CREATE OR REPLACE FUNCTION seed_logged_workout(
    p_user_id uuid,
    p_workout_id uuid,
    p_scheduled_at timestamptz,
    p_completed_at timestamptz,
    p_ai_modified boolean,
    p_notes text,
    p_entry_id uuid DEFAULT NULL,
    p_log_id uuid DEFAULT NULL,
    p_max_order_index integer DEFAULT NULL,
    p_rpe_mode text DEFAULT 'session',
    p_rpe_seed integer DEFAULT NULL,
    p_progress_factor integer DEFAULT 0
) RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_entry uuid;
    v_log uuid;
    v_exercise_mode constant text := 'exercise';
BEGIN
    INSERT INTO scheduled_entries (entry_id, user_id, workout_id, scheduled, status)
    VALUES (COALESCE(p_entry_id, gen_random_uuid()), p_user_id, p_workout_id, p_scheduled_at, 'Completed')
    RETURNING entry_id INTO v_entry;

    INSERT INTO workout_logs (log_id, entry_id, started_at, completed_at, ai_modified, notes)
    VALUES (COALESCE(p_log_id, gen_random_uuid()), v_entry, p_scheduled_at, p_completed_at, p_ai_modified, p_notes)
    RETURNING log_id INTO v_log;

    INSERT INTO workout_log_exercises (
        log_exercise_id, log_id, exercise_id, workout_exercise_id, order_index, group_number)
    SELECT
        gen_random_uuid(),
        v_log,
        we.exercise_dict_id,
        we.workout_exercise_id,
        we.order_index,
        CASE
            WHEN we.group_id IS NULL THEN 0
            ELSE DENSE_RANK() OVER (PARTITION BY we.workout_id ORDER BY we.group_id)
        END
    FROM workout_exercises we
    WHERE we.workout_id = p_workout_id
      AND (p_max_order_index IS NULL OR we.order_index <= p_max_order_index);

    INSERT INTO workout_log_sets (
        log_set_id, log_id, exercise_id, workout_exercise_id, set_id, set_type, reps, weight, duration, distance, rest_time, group_number, rpe, order_index, ai_suggested, logged_at)
    SELECT
        gen_random_uuid(),
        v_log,
        we.exercise_dict_id,
        we.workout_exercise_id,
        s.set_id,
        s.set_type,
        COALESCE(s.reps + p_progress_factor, s.duration, ROUND(s.distance)::int, 1),
        COALESCE(s.weight, 0),
        s.duration,
        s.distance,
        s.rest_time,
        CASE
            WHEN we.group_id IS NULL THEN 0
            ELSE DENSE_RANK() OVER (PARTITION BY we.workout_id ORDER BY we.group_id)
        END,
        CASE
            WHEN p_rpe_mode = v_exercise_mode THEN
                CASE
                    WHEN we.order_index % 3 = 0 THEN 7.5
                    WHEN we.order_index % 3 = 1 THEN 8.0
                    ELSE 8.5
                END
            ELSE
                CASE
                    WHEN COALESCE(p_rpe_seed, 0) % 3 = 0 THEN 7.5
                    WHEN COALESCE(p_rpe_seed, 0) % 3 = 1 THEN 8.0
                    ELSE 8.5
                END
        END,
        s.order_index,
        false,
        p_scheduled_at + (we.order_index * INTERVAL '4 minutes') + (s.order_index * INTERVAL '45 seconds')
    FROM workout_exercises we
    JOIN sets s ON s.workout_exercise_id = we.workout_exercise_id
    WHERE we.workout_id = p_workout_id
      AND (p_max_order_index IS NULL OR we.order_index <= p_max_order_index);
END;
$$;

DO $$
DECLARE
    test_email text;
    hex_enc text;
    exercise_mode text;
    completed_status constant text := 'Completed';
    push_name constant text := 'Push Day A';
    upper_name constant text := 'Upper B';
    test_id uuid;
    v_push uuid;
    v_upper uuid;
    rec record;
BEGIN
    SELECT c.test_user_email INTO test_email
    FROM seed_constants c
    LIMIT 1;

    SELECT c.hex_enc INTO hex_enc
    FROM seed_constants c
    LIMIT 1;

    SELECT c.rpe_exercise INTO exercise_mode
    FROM seed_constants c
    LIMIT 1;

    SELECT user_id INTO test_id
    FROM users
    WHERE email_hash = encode(sha256(test_email::bytea), hex_enc);

    IF test_id IS NULL THEN
        RAISE NOTICE 'Test user (%) not found - run the C# seeder (dotnet run) before this script.', test_email;
        RETURN;
    END IF;

    DELETE FROM workout_log_sets
    WHERE log_id IN (
        SELECT log_id
        FROM workout_logs
        WHERE entry_id IN (
            SELECT entry_id
            FROM scheduled_entries
            WHERE user_id = test_id
              AND workout_id IN (
                  SELECT workout_id
                  FROM workouts
                  WHERE user_id = test_id AND name IN (push_name, upper_name)
              )
              AND status = completed_status
        )
    );

    DELETE FROM workout_logs
    WHERE entry_id IN (
        SELECT entry_id
        FROM scheduled_entries
        WHERE user_id = test_id
          AND workout_id IN (
              SELECT workout_id
              FROM workouts
              WHERE user_id = test_id AND name IN (push_name, upper_name)
          )
          AND status = completed_status
    );

    DELETE FROM scheduled_entries
    WHERE user_id = test_id
      AND workout_id IN (
          SELECT workout_id
          FROM workouts
          WHERE user_id = test_id AND name IN (push_name, upper_name)
      )
      AND status = completed_status;

    SELECT workout_id INTO v_push
    FROM workouts
    WHERE user_id = test_id AND name = push_name
    LIMIT 1;

    SELECT workout_id INTO v_upper
    FROM workouts
    WHERE user_id = test_id AND name = upper_name
    LIMIT 1;

    IF v_push IS NULL OR v_upper IS NULL THEN
        RAISE NOTICE 'Test user split workouts not found - run the demo workout seeding before this block.';
        RETURN;
    END IF;

    FOR rec IN
        SELECT * FROM (VALUES
            (TIMESTAMPTZ '2026-06-10 18:30:00+00', v_push),
            (TIMESTAMPTZ '2026-06-12 18:30:00+00', v_upper)
        ) AS t(scheduled_at, workout_id)
    LOOP
        PERFORM seed_logged_workout(
            test_id,
            rec.workout_id,
            rec.scheduled_at,
            rec.scheduled_at + INTERVAL '55 minutes',
            false,
            NULL,
            CASE WHEN rec.workout_id = v_push THEN 'd6d19f21-8c17-49d1-b7eb-7a8c59dca1cd'::uuid ELSE NULL END,
            CASE WHEN rec.workout_id = v_push THEN '58597dd0-e02c-416c-a4b0-cba560f21045'::uuid ELSE NULL END,
            NULL,
            exercise_mode,
            NULL
        );
    END LOOP;
END $$;

DO $$
DECLARE
    alex_email text;
    hex_enc text;
    exercise_mode text;
    normal_set_type text;
    pull_name constant text := 'Pull';
    push_name constant text := 'Push';
    legs_name constant text := 'Legs';
    my_split_name constant text := 'My Split';
    alex_id uuid;
    v_folder uuid;
    v_pull uuid;
    v_push uuid;
    v_legs uuid;
    v_we uuid;
    v_ex uuid;
    v_day timestamp;
    i int;
    rec record;
BEGIN
    SELECT c.alex_user_email INTO alex_email
    FROM seed_constants c
    LIMIT 1;

    SELECT c.hex_enc, c.set_type
    INTO hex_enc, normal_set_type
    FROM seed_constants c
    LIMIT 1;

    SELECT c.rpe_exercise INTO exercise_mode
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
            WHERE user_id = alex_id AND name IN (pull_name, push_name, legs_name)
        )
    );

    DELETE FROM workout_exercises
    WHERE workout_id IN (
        SELECT workout_id
        FROM workouts
        WHERE user_id = alex_id AND name IN (pull_name, push_name, legs_name)
    );

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

    SELECT workout_id INTO v_legs
    FROM workouts
    WHERE user_id = alex_id AND name = legs_name
    LIMIT 1;

    IF v_legs IS NULL THEN
        INSERT INTO workouts (workout_id, folder_id, name, user_id, created_at)
        VALUES (gen_random_uuid(), v_folder, legs_name, alex_id, NOW())
        RETURNING workout_id INTO v_legs;
    END IF;

    -- exercises + sets for each recent-workout card (volume = weight*reps, count = #sets)
    FOR rec IN
        SELECT t.* FROM seed_constants c
        CROSS JOIN LATERAL (VALUES
            (v_pull, 'Cable lat pulldown full range of motion',            1, 5, 12, 45::real),
            (v_pull, 'Cable seated row',        2, 4, 10, 50::real),
            (v_pull, c.exercise_pull_up_name,   3, 4, 8,   0::real),
            (v_pull, 'Dumbbell Alternate Bicep Curl',     4, 3, 12, 14::real),
            (v_push, c.exercise_bench_press_name, 1, 4, 8,  60::real),
            (v_push, 'Barbell seated overhead press',          2, 4, 8,  40::real),
            (v_push, 'Dumbbell incline bench press',   3, 4, 10, 30::real),
            (v_push, 'Cable triceps pushdown (v-bar)',          4, 4, 12, 25::real),
            (v_legs, 'Barbell Back Squat', 1, 4, 6, 100::real),
            (v_legs, 'Barbell romanian deadlift', 2, 4, 8, 80::real),
            (v_legs, 'Walking lunge', 3, 3, 10, 20::real),
            (v_legs, 'Standing calf raise', 4, 4, 15, 60::real)
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
        SELECT gen_random_uuid(), v_we, normal_set_type, rec.reps, rec.weight, NULL, NULL, gs, 90
        FROM generate_series(1, rec.n_sets) AS gs;
    END LOOP;

    -- makes it such that the user always has a 6 week long streak
    FOR i IN 0..23 LOOP
        v_day := NOW() - INTERVAL '42 days' + (i * INTERVAL '41 hours');
        PERFORM seed_logged_workout(
            alex_id,
            CASE WHEN i % 3 = 0 THEN v_push WHEN i % 3 = 1 THEN v_pull ELSE v_legs END,
            v_day,
            v_day + INTERVAL '65 minutes',
            false,
            NULL,
            NULL,
            NULL,
            NULL,
            exercise_mode,
            NULL,
            (i / 3)
        );
    END LOOP;
END $$;

-- ===========================================================================
-- Alex's upcoming schedule. Reuses Alex's own workouts and stays idempotent
-- per (user, workout, scheduled, status) row.
-- ===========================================================================
DO $$
DECLARE
    alex_email text;
    hex_enc text;
    pull_name constant text := 'Pull';
    push_name constant text := 'Push';
    scheduled_status constant text := 'Scheduled';
    alex_id uuid;
    v_pull uuid;
    v_push uuid;
    rec record;
BEGIN
    SELECT c.alex_user_email INTO alex_email
    FROM seed_constants c
    LIMIT 1;

    SELECT c.hex_enc INTO hex_enc
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


DELETE FROM exercise_prs
WHERE user_id IN (
    SELECT u.user_id
    FROM users u
    CROSS JOIN seed_constants c
    WHERE u.email_hash IN (
        encode(sha256(c.test_user_email::bytea), c.hex_enc),
        encode(sha256(c.demo_user_email::bytea), c.hex_enc),
        encode(sha256(c.alex_user_email::bytea), c.hex_enc)
    )
);

INSERT INTO exercise_prs (pr_id, user_id, exercise_id, workout_log_set_id, pr_type, pr_value, achieved_weight, achieved_reps)
WITH DemoUsers AS (
    SELECT u.user_id
    FROM users u
    CROSS JOIN seed_constants c
    WHERE u.email_hash IN (
        encode(sha256(c.test_user_email::bytea), c.hex_enc),
        encode(sha256(c.demo_user_email::bytea), c.hex_enc),
        encode(sha256(c.alex_user_email::bytea), c.hex_enc)
    )
),
UserSets AS (
    SELECT 
        se.user_id,
        wls.exercise_id,
        wls.log_set_id,
        wls.weight,
        wls.reps,
        (wls.weight * wls.reps) as volume
    FROM workout_log_sets wls
    JOIN workout_logs wl ON wl.log_id = wls.log_id
    JOIN scheduled_entries se ON se.entry_id = wl.entry_id
    JOIN DemoUsers du ON du.user_id = se.user_id
),
MaxWeights AS (
    SELECT user_id, exercise_id, MAX(weight) as max_weight
    FROM UserSets
    GROUP BY user_id, exercise_id
)
SELECT DISTINCT ON (mw.user_id, mw.exercise_id)
    gen_random_uuid(),
    us.user_id,
    us.exercise_id,
    us.log_set_id,
    0, -- MaxWeight
    us.weight,
    us.weight,
    us.reps
FROM MaxWeights mw
JOIN UserSets us ON us.user_id = mw.user_id AND us.exercise_id = mw.exercise_id AND us.weight = mw.max_weight
ON CONFLICT DO NOTHING;

INSERT INTO exercise_prs (pr_id, user_id, exercise_id, workout_log_set_id, pr_type, pr_value, achieved_weight, achieved_reps)
WITH DemoUsers AS (
    SELECT u.user_id
    FROM users u
    CROSS JOIN seed_constants c
    WHERE u.email_hash IN (
        encode(sha256(c.test_user_email::bytea), c.hex_enc),
        encode(sha256(c.demo_user_email::bytea), c.hex_enc),
        encode(sha256(c.alex_user_email::bytea), c.hex_enc)
    )
),
UserSets AS (
    SELECT 
        se.user_id,
        wls.exercise_id,
        wls.log_set_id,
        wls.weight,
        wls.reps,
        (wls.weight * wls.reps) as volume
    FROM workout_log_sets wls
    JOIN workout_logs wl ON wl.log_id = wls.log_id
    JOIN scheduled_entries se ON se.entry_id = wl.entry_id
    JOIN DemoUsers du ON du.user_id = se.user_id
),
MaxVolumes AS (
    SELECT user_id, exercise_id, MAX(volume) as max_volume
    FROM UserSets
    GROUP BY user_id, exercise_id
)
SELECT DISTINCT ON (mv.user_id, mv.exercise_id)
    gen_random_uuid(),
    us.user_id,
    us.exercise_id,
    us.log_set_id,
    1, -- set volume
    us.volume,
    us.weight,
    us.reps
FROM MaxVolumes mv
JOIN UserSets us ON us.user_id = mv.user_id AND us.exercise_id = mv.exercise_id AND us.volume = mv.max_volume
ON CONFLICT DO NOTHING;

COMMIT;
