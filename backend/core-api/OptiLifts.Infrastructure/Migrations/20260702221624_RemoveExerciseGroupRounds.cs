using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExerciseGroupRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rounds",
                table: "exercise_groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "rounds",
                table: "exercise_groups",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
