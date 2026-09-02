using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChronicFatigueAddRpeTrend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fatigue_states");

            migrationBuilder.AddColumn<bool>(
                name: "rpe_trend_rising",
                table: "exercise_trends",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rpe_trend_rising",
                table: "exercise_trends");

            migrationBuilder.CreateTable(
                name: "fatigue_states",
                columns: table => new
                {
                    fatigue_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acute_load = table.Column<float>(type: "real", nullable: false),
                    acwr = table.Column<float>(type: "real", nullable: false),
                    chronic_load = table.Column<float>(type: "real", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confidence = table.Column<string>(type: "text", nullable: false),
                    decrement_ratio = table.Column<float>(type: "real", nullable: false),
                    is_flagged = table.Column<bool>(type: "boolean", nullable: false),
                    rpe_slope = table.Column<float>(type: "real", nullable: false),
                    signals_fired = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_fatigue_states_user_id",
                table: "fatigue_states",
                column: "user_id",
                unique: true);
        }
    }
}
