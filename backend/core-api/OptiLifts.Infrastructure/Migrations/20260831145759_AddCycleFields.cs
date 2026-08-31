using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cycle_start_date",
                table: "user_schedule_config",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "cycle_window_length_days",
                table: "user_schedule_config",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cycle_start_date",
                table: "user_schedule_config");

            migrationBuilder.DropColumn(
                name: "cycle_window_length_days",
                table: "user_schedule_config");
        }
    }
}
