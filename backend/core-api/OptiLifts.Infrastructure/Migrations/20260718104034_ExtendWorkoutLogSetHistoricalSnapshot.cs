using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendWorkoutLogSetHistoricalSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workout_log_sets_sets_set_id",
                table: "workout_log_sets");

            migrationBuilder.DropIndex(
                name: "IX_workout_log_sets_set_id",
                table: "workout_log_sets");

            migrationBuilder.AddColumn<float>(
                name: "distance",
                table: "workout_log_sets",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration",
                table: "workout_log_sets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "group_number",
                table: "workout_log_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "rest_time",
                table: "workout_log_sets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "workout_exercise_id",
                table: "workout_log_sets",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                WITH workout_group_numbers AS (
                    SELECT
                        we.workout_exercise_id,
                        CASE
                            WHEN we.group_id IS NULL THEN 0
                            ELSE DENSE_RANK() OVER (PARTITION BY we.workout_id ORDER BY we.group_id)
                        END AS group_number
                    FROM workout_exercises we
                )
                UPDATE workout_log_sets wls
                SET
                    workout_exercise_id = s.workout_exercise_id,
                    duration = s.duration,
                    distance = s.distance,
                    rest_time = s.rest_time,
                    group_number = COALESCE(wgn.group_number, 0)
                FROM sets s
                LEFT JOIN workout_group_numbers wgn ON wgn.workout_exercise_id = s.workout_exercise_id
                WHERE wls.set_id = s.set_id;
            ");

            migrationBuilder.Sql(@"
                WITH workout_group_numbers AS (
                    SELECT
                        we.workout_exercise_id,
                        CASE
                            WHEN we.group_id IS NULL THEN 0
                            ELSE DENSE_RANK() OVER (PARTITION BY we.workout_id ORDER BY we.group_id)
                        END AS group_number
                    FROM workout_exercises we
                )
                INSERT INTO workout_log_sets (
                    log_set_id,
                    log_id,
                    exercise_id,
                    workout_exercise_id,
                    set_id,
                    set_type,
                    reps,
                    weight,
                    duration,
                    distance,
                    rest_time,
                    group_number,
                    rpe,
                    order_index,
                    ai_suggested,
                    logged_at
                )
                SELECT
                    gen_random_uuid(),
                    wl.log_id,
                    we.exercise_dict_id,
                    s.workout_exercise_id,
                    s.set_id,
                    s.set_type,
                    COALESCE(s.reps, s.duration, CASE WHEN s.distance IS NULL THEN NULL ELSE ROUND(s.distance)::int END, 0),
                    COALESCE(s.weight, 0),
                    s.duration,
                    s.distance,
                    s.rest_time,
                    COALESCE(wgn.group_number, 0),
                    0,
                    s.order_index,
                    false,
                    COALESCE(wl.completed_at, wl.started_at)
                FROM workout_logs wl
                JOIN scheduled_entries se ON se.entry_id = wl.entry_id
                JOIN workout_exercises we ON we.workout_id = se.workout_id
                JOIN sets s ON s.workout_exercise_id = we.workout_exercise_id
                LEFT JOIN workout_group_numbers wgn ON wgn.workout_exercise_id = we.workout_exercise_id
                LEFT JOIN workout_log_sets existing
                    ON existing.log_id = wl.log_id
                    AND existing.set_id = s.set_id
                WHERE existing.log_set_id IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "distance",
                table: "workout_log_sets");

            migrationBuilder.DropColumn(
                name: "duration",
                table: "workout_log_sets");

            migrationBuilder.DropColumn(
                name: "group_number",
                table: "workout_log_sets");

            migrationBuilder.DropColumn(
                name: "rest_time",
                table: "workout_log_sets");

            migrationBuilder.DropColumn(
                name: "workout_exercise_id",
                table: "workout_log_sets");

            migrationBuilder.CreateIndex(
                name: "IX_workout_log_sets_set_id",
                table: "workout_log_sets",
                column: "set_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workout_log_sets_sets_set_id",
                table: "workout_log_sets",
                column: "set_id",
                principalTable: "sets",
                principalColumn: "set_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
