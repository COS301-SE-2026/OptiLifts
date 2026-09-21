using System;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptiClashLeaderboardsAndSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "arenas",
                columns: table => new
                {
                    arena_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metric_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    season_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arenas", x => x.arena_id);
                    table.ForeignKey(
                        name: "FK_arenas_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "athlete_profile_kudos",
                columns: table => new
                {
                    kudos_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_athlete_profile_kudos", x => x.kudos_id);
                    table.ForeignKey(
                        name: "FK_athlete_profile_kudos_users_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_athlete_profile_kudos_users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "athlete_season_snapshots",
                columns: table => new
                {
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_key = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    bodyweight_kg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_opted_in = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    squat_1rm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 0m),
                    bench_1rm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 0m),
                    deadlift_1rm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 0m),
                    total_e1rm = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false, defaultValue: 0m),
                    dots_score = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false, defaultValue: 0m),
                    tier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Bronze"),
                    tier_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    rank_trend = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    weekly_volume_kg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    last_workout_date = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_athlete_season_snapshots", x => x.snapshot_id);
                    table.ForeignKey(
                        name: "FK_athlete_season_snapshots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_arenas_code",
                table: "arenas",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_arenas_created_by_id",
                table: "arenas",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_arenas_type",
                table: "arenas",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_athlete_profile_kudos_sender_user_id",
                table: "athlete_profile_kudos",
                column: "sender_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_athlete_profile_kudos_target_user_id_sender_user_id",
                table: "athlete_profile_kudos",
                columns: new[] { "target_user_id", "sender_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_athlete_season_snapshots_season_key_bench_1rm",
                table: "athlete_season_snapshots",
                columns: new[] { "season_key", "bench_1rm" });

            migrationBuilder.CreateIndex(
                name: "IX_athlete_season_snapshots_season_key_deadlift_1rm",
                table: "athlete_season_snapshots",
                columns: new[] { "season_key", "deadlift_1rm" });

            migrationBuilder.CreateIndex(
                name: "IX_athlete_season_snapshots_season_key_is_opted_in_gender_body~",
                table: "athlete_season_snapshots",
                columns: new[] { "season_key", "is_opted_in", "gender", "bodyweight_kg", "dots_score" });

            migrationBuilder.CreateIndex(
                name: "IX_athlete_season_snapshots_season_key_squat_1rm",
                table: "athlete_season_snapshots",
                columns: new[] { "season_key", "squat_1rm" });

            migrationBuilder.CreateIndex(
                name: "IX_athlete_season_snapshots_season_key_weekly_volume_kg",
                table: "athlete_season_snapshots",
                columns: new[] { "season_key", "weekly_volume_kg" });

            migrationBuilder.CreateIndex(
                name: "IX_athlete_season_snapshots_user_id_season_key",
                table: "athlete_season_snapshots",
                columns: new[] { "user_id", "season_key" },
                unique: true);

            migrationBuilder.InsertData(
                table: "arenas",
                columns: new[] { "arena_id", "name", "type", "metric_type", "duration_days", "season_end_date", "created_at" },
                values: new object[,]
                {
                    { "global-league", "OptiLifts Global League", "Global", "DotsOverall", 30, new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc), new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "weight-class-league", "Divisional Weight-Class League", "Divisional", "DotsOverall", 30, new DateTime(2026, 9, 30,23, 59, 59, DateTimeKind.Utc), new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc) }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "arenas");

            migrationBuilder.DropTable(
                name: "athlete_profile_kudos");

            migrationBuilder.DropTable(
                name: "athlete_season_snapshots");
        }
    }
}
