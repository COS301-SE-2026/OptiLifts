using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRepRangeDefaultsForCompoundIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE user_rep_range
                SET exercise_type = 'Compound'
                WHERE exercise_type NOT IN ('Compound', 'Isolation');
                """);

            migrationBuilder.AlterColumn<int>(
                name: "upper_limit",
                table: "user_rep_range",
                type: "integer",
                nullable: false,
                defaultValue: 10,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "lower_limit",
                table: "user_rep_range",
                type: "integer",
                nullable: false,
                defaultValue: 8,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_rep_range_bounds",
                table: "user_rep_range",
                sql: "lower_limit <= upper_limit");

            migrationBuilder.AddCheckConstraint(
                name: "CK_user_rep_range_exercise_type",
                table: "user_rep_range",
                sql: "exercise_type IN ('Compound', 'Isolation')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_user_rep_range_bounds",
                table: "user_rep_range");

            migrationBuilder.DropCheckConstraint(
                name: "CK_user_rep_range_exercise_type",
                table: "user_rep_range");

            migrationBuilder.AlterColumn<int>(
                name: "upper_limit",
                table: "user_rep_range",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 10);

            migrationBuilder.AlterColumn<int>(
                name: "lower_limit",
                table: "user_rep_range",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 8);
        }
    }
}
