using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseEstimationAndUserRepRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercise_estimation",
                columns: table => new
                {
                    estimate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_dict_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weight = table.Column<float>(type: "real", nullable: true),
                    reps = table.Column<int>(type: "integer", nullable: false),
                    exercise_type = table.Column<string>(type: "text", nullable: false),
                    time_stamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deload = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_estimation", x => x.estimate_id);
                    table.ForeignKey(
                        name: "FK_exercise_estimation_exercise_dictionary_exercise_dict_id",
                        column: x => x.exercise_dict_id,
                        principalTable: "exercise_dictionary",
                        principalColumn: "exercise_dict_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exercise_estimation_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_rep_range",
                columns: table => new
                {
                    rep_range_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_type = table.Column<string>(type: "text", nullable: false),
                    lower_limit = table.Column<int>(type: "integer", nullable: false),
                    upper_limit = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_rep_range", x => x.rep_range_id);
                    table.ForeignKey(
                        name: "FK_user_rep_range_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_estimation_exercise_dict_id",
                table: "exercise_estimation",
                column: "exercise_dict_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_estimation_user_id_exercise_dict_id_time_stamp",
                table: "exercise_estimation",
                columns: new[] { "user_id", "exercise_dict_id", "time_stamp" });

            migrationBuilder.CreateIndex(
                name: "IX_user_rep_range_user_id_exercise_type",
                table: "user_rep_range",
                columns: new[] { "user_id", "exercise_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_estimation");

            migrationBuilder.DropTable(
                name: "user_rep_range");
        }
    }
}
