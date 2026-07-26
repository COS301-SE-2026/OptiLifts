using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exercise_dictionary_user_id",
                table: "exercise_dictionary");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "exercise_dictionary",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_exercise_dictionary_user_id_is_deleted",
                table: "exercise_dictionary",
                columns: new[] { "user_id", "is_deleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exercise_dictionary_user_id_is_deleted",
                table: "exercise_dictionary");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "exercise_dictionary");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_dictionary_user_id",
                table: "exercise_dictionary",
                column: "user_id");
        }
    }
}
