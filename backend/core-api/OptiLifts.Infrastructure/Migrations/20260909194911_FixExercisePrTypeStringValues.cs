using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixExercisePrTypeStringValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                UPDATE exercise_prs SET pr_type = 'MaxWeight' WHERE pr_type = '0';
                UPDATE exercise_prs SET pr_type = 'MaxSetVolume' WHERE pr_type = '1';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                UPDATE exercise_prs SET pr_type = '0' WHERE pr_type = 'MaxWeight';
                UPDATE exercise_prs SET pr_type = '1' WHERE pr_type = 'MaxSetVolume';
            ");
        }
    }
}
