-- Wipes all user data, run by DatabaseWiper once per WIPE_DATA_TOKEN value, inside a
-- transaction, before seeding.
--
-- Deletes: every user and everything they own (workouts, logs, schedules, PRs, trends,
-- badges earned, custom exercises, friends, arenas, duels, OptiVision jobs, ...).
-- Keeps:   the exercise dictionary, secondary muscles, muscles, badge definitions,
--          the two global arenas and the migration history.

-- Deleting users also deletes exercises owned by those users (custom exercises). The
-- built-in dictionary must never be owned by an existing user, so stop (and roll the
-- whole wipe back) if an unexpectedly large number of exercises would go with them.
DO $$
DECLARE
    owned int;
BEGIN
    SELECT count(*) INTO owned
    FROM exercise_dictionary
    WHERE user_id IN (SELECT user_id FROM users);

    IF owned > 50 THEN
        RAISE EXCEPTION 'Wipe stopped: % exercises belong to existing users and would be deleted. Nothing was changed.', owned;
    END IF;
END $$;

-- all user-owned data. Listed explicitly (no CASCADE) so the exercise dictionary,
-- which references users, can never be truncated by accident
TRUNCATE
    workout_log_sets, workout_log_exercises, workout_logs, scheduled_entries,
    sets, exercise_groups, workout_exercises, workouts, folders,
    exercise_prs, exercise_trends, exercise_estimation, training_events,
    user_badges, user_models, user_rep_range, user_schedule_config,
    vision_analysis_jobs, messages,
    friend_requests, friendships,
    duel_timeline_events, duels,
    clash_activity_kudos, clash_activities, arena_invites, arena_members,
    athlete_profile_kudos, athlete_season_snapshots;

-- private arenas (the global and divisional leagues are created by a migration)
DELETE FROM arenas WHERE arena_id NOT IN ('global-league', 'weight-class-league');

-- custom exercises owned by these users are removed with them (ON DELETE CASCADE)
DELETE FROM users; --NOSONAR
