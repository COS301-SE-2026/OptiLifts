using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExercisePrs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercise_prs",
                columns: table => new
                {
                    pr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pr_type = table.Column<string>(type: "text", nullable: false),
                    pr_value = table.Column<float>(type: "real", nullable: false),
                    achieved_weight = table.Column<float>(type: "real", nullable: false),
                    achieved_reps = table.Column<int>(type: "integer", nullable: false),
                    achieved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_prs", x => x.pr_id);
                    table.ForeignKey(
                        name: "FK_exercise_prs_exercise_dictionary_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise_dictionary",
                        principalColumn: "exercise_dict_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exercise_prs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_prs_exercise_id",
                table: "exercise_prs",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_prs_user_id_exercise_id_pr_type",
                table: "exercise_prs",
                columns: new[] { "user_id", "exercise_id", "pr_type" },
                unique: true,
                filter: "is_current = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_prs");
        }
    }
}
