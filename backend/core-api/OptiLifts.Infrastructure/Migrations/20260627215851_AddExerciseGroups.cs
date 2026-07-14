using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "workout_exercises",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "exercise_groups",
                columns: table => new
                {
                    exercise_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_type = table.Column<string>(type: "text", nullable: false),
                    rounds = table.Column<int>(type: "integer", nullable: false),
                    rest_time = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_groups", x => x.exercise_group_id);
                    table.ForeignKey(
                        name: "FK_exercise_groups_workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_group_id",
                table: "workout_exercises",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_groups_workout_id",
                table: "exercise_groups",
                column: "workout_id");

            migrationBuilder.AddForeignKey(
                name: "FK_workout_exercises_exercise_groups_group_id",
                table: "workout_exercises",
                column: "group_id",
                principalTable: "exercise_groups",
                principalColumn: "exercise_group_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workout_exercises_exercise_groups_group_id",
                table: "workout_exercises");

            migrationBuilder.DropTable(
                name: "exercise_groups");

            migrationBuilder.DropIndex(
                name: "IX_workout_exercises_group_id",
                table: "workout_exercises");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "workout_exercises");
        }
    }
}
