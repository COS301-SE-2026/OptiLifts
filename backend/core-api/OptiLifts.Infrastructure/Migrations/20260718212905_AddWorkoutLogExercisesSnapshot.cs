using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutLogExercisesSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workout_log_exercises",
                columns: table => new
                {
                    log_exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_exercise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    group_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_log_exercises", x => x.log_exercise_id);
                    table.ForeignKey(
                        name: "FK_workout_log_exercises_exercise_dictionary_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise_dictionary",
                        principalColumn: "exercise_dict_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_log_exercises_workout_logs_log_id",
                        column: x => x.log_id,
                        principalTable: "workout_logs",
                        principalColumn: "log_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
                INSERT INTO workout_log_exercises (
                    log_exercise_id,
                    log_id,
                    exercise_id,
                    workout_exercise_id,
                    order_index,
                    group_number
                )
                SELECT
                    gen_random_uuid(),
                    grouped.log_id,
                    grouped.exercise_id,
                    grouped.workout_exercise_id,
                    grouped.exercise_order_index,
                    grouped.group_number
                FROM (
                    SELECT
                        wls.log_id,
                        wls.exercise_id,
                        wls.workout_exercise_id,
                        MIN(COALESCE(wls.order_index, 0)) AS exercise_order_index,
                        COALESCE(MAX(wls.group_number), 0) AS group_number
                    FROM workout_log_sets wls
                    GROUP BY wls.log_id, wls.exercise_id, wls.workout_exercise_id
                ) grouped;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_workout_log_exercises_exercise_id",
                table: "workout_log_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_log_exercises_log_id_exercise_id",
                table: "workout_log_exercises",
                columns: new[] { "log_id", "exercise_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_log_exercises_log_id_order_index",
                table: "workout_log_exercises",
                columns: new[] { "log_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_log_exercises_log_id_workout_exercise_id",
                table: "workout_log_exercises",
                columns: new[] { "log_id", "workout_exercise_id" },
                unique: true,
                filter: "workout_exercise_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_log_exercises");
        }
    }
}
