using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOptiLiftsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    force = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<string>(type: "text", nullable: false),
                    mechanic = table.Column<string>(type: "text", nullable: true),
                    equipment = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "text", nullable: false),
                    primary_muscles = table.Column<List<string>>(type: "text[]", nullable: false),
                    secondary_muscles = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercises", x => x.exercise_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "folders",
                columns: table => new
                {
                    folder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folders", x => x.folder_id);
                    table.ForeignKey(
                        name: "FK_folders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workouts",
                columns: table => new
                {
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    folder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    day_index = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workouts", x => x.workout_id);
                    table.ForeignKey(
                        name: "FK_workouts_folders_folder_id",
                        column: x => x.folder_id,
                        principalTable: "folders",
                        principalColumn: "folder_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workouts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sets",
                columns: table => new
                {
                    set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    set_type = table.Column<string>(type: "text", nullable: false),
                    reps = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<float>(type: "real", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    rest_time = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sets", x => x.set_id);
                    table.ForeignKey(
                        name: "FK_sets_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sets_workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_logs",
                columns: table => new
                {
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ai_modified = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_logs", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_workout_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_logs_workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "workout_log_sets",
                columns: table => new
                {
                    log_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    set_type = table.Column<string>(type: "text", nullable: false),
                    reps = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<float>(type: "real", nullable: false),
                    rpe = table.Column<float>(type: "real", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    ai_suggested = table.Column<bool>(type: "boolean", nullable: false),
                    logged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_log_sets", x => x.log_set_id);
                    table.ForeignKey(
                        name: "FK_workout_log_sets_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_log_sets_sets_set_id",
                        column: x => x.set_id,
                        principalTable: "sets",
                        principalColumn: "set_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_workout_log_sets_workout_logs_log_id",
                        column: x => x.log_id,
                        principalTable: "workout_logs",
                        principalColumn: "log_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_folders_user_id",
                table: "folders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sets_exercise_id",
                table: "sets",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_sets_workout_id",
                table: "sets",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_log_sets_exercise_id",
                table: "workout_log_sets",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_log_sets_log_id",
                table: "workout_log_sets",
                column: "log_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_log_sets_set_id",
                table: "workout_log_sets",
                column: "set_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_logs_user_id",
                table: "workout_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_logs_workout_id",
                table: "workout_logs",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "IX_workouts_created_by",
                table: "workouts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_workouts_folder_id",
                table: "workouts",
                column: "folder_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workout_log_sets");

            migrationBuilder.DropTable(
                name: "sets");

            migrationBuilder.DropTable(
                name: "workout_logs");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "workouts");

            migrationBuilder.DropTable(
                name: "folders");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
