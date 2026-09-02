using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlateauAndFatigueDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "rpe",
                table: "workout_log_sets",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.CreateTable(
                name: "exercise_trends",
                columns: table => new
                {
                    trend_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slope_pct_per_week = table.Column<float>(type: "real", nullable: false),
                    slope_ci_low = table.Column<float>(type: "real", nullable: false),
                    slope_ci_high = table.Column<float>(type: "real", nullable: false),
                    mean_e1rm = table.Column<float>(type: "real", nullable: false),
                    sessions_used = table.Column<int>(type: "integer", nullable: false),
                    window_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    window_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    supersedes_exercise_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_trends", x => x.trend_id);
                    table.ForeignKey(
                        name: "FK_exercise_trends_exercise_dictionary_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise_dictionary",
                        principalColumn: "exercise_dict_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exercise_trends_exercise_dictionary_supersedes_exercise_id",
                        column: x => x.supersedes_exercise_id,
                        principalTable: "exercise_dictionary",
                        principalColumn: "exercise_dict_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_exercise_trends_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fatigue_states",
                columns: table => new
                {
                    fatigue_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acute_load = table.Column<float>(type: "real", nullable: false),
                    chronic_load = table.Column<float>(type: "real", nullable: false),
                    acwr = table.Column<float>(type: "real", nullable: false),
                    rpe_slope = table.Column<float>(type: "real", nullable: false),
                    decrement_ratio = table.Column<float>(type: "real", nullable: false),
                    signals_fired = table.Column<int>(type: "integer", nullable: false),
                    is_flagged = table.Column<bool>(type: "boolean", nullable: false),
                    confidence = table.Column<string>(type: "text", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fatigue_states", x => x.fatigue_state_id);
                    table.ForeignKey(
                        name: "FK_fatigue_states_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false),
                    diagnosis = table.Column<string>(type: "text", nullable: true),
                    confidence = table.Column<float>(type: "real", nullable: true),
                    recommendation = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_events", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_training_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_trends_exercise_id",
                table: "exercise_trends",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_trends_supersedes_exercise_id",
                table: "exercise_trends",
                column: "supersedes_exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_trends_user_id_exercise_id",
                table: "exercise_trends",
                columns: new[] { "user_id", "exercise_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fatigue_states_user_id",
                table: "fatigue_states",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_events_user_id_created_at",
                table: "training_events",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_trends");

            migrationBuilder.DropTable(
                name: "fatigue_states");

            migrationBuilder.DropTable(
                name: "training_events");

            migrationBuilder.AlterColumn<float>(
                name: "rpe",
                table: "workout_log_sets",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);
        }
    }
}
