using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptiClashDuels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "duels",
                columns: table => new
                {
                    duel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    challenger_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rival_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    exercise_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    is_draw = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    challenger_baseline_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    rival_baseline_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    challenger_current_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    rival_current_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    winner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duels", x => x.duel_id);
                    table.ForeignKey(
                        name: "FK_duels_exercise_dictionary_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise_dictionary",
                        principalColumn: "exercise_dict_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_duels_users_challenger_user_id",
                        column: x => x.challenger_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_duels_users_rival_user_id",
                        column: x => x.rival_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_duels_users_winner_user_id",
                        column: x => x.winner_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "duel_timeline_events",
                columns: table => new
                {
                    timeline_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_log_set_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_text = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_pr = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duel_timeline_events", x => x.timeline_event_id);
                    table.ForeignKey(
                        name: "FK_duel_timeline_events_duels_duel_id",
                        column: x => x.duel_id,
                        principalTable: "duels",
                        principalColumn: "duel_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_duel_timeline_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_duel_timeline_events_workout_log_sets_workout_log_set_id",
                        column: x => x.workout_log_set_id,
                        principalTable: "workout_log_sets",
                        principalColumn: "log_set_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_duel_timeline_events_duel_id_created_at",
                table: "duel_timeline_events",
                columns: new[] { "duel_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_duel_timeline_events_user_id",
                table: "duel_timeline_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_duel_timeline_events_workout_log_set_id",
                table: "duel_timeline_events",
                column: "workout_log_set_id");

            migrationBuilder.CreateIndex(
                name: "IX_duels_challenger_user_id_status",
                table: "duels",
                columns: new[] { "challenger_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_duels_exercise_id",
                table: "duels",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_duels_rival_user_id_status",
                table: "duels",
                columns: new[] { "rival_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_duels_status_end_date",
                table: "duels",
                columns: new[] { "status", "end_date" },
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_duels_winner_user_id",
                table: "duels",
                column: "winner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duel_timeline_events");

            migrationBuilder.DropTable(
                name: "duels");
        }
    }
}
