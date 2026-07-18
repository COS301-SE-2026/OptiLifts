using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workouts_user_id",
                table: "workouts");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "workouts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "workouts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_workouts_user_id_is_deleted",
                table: "workouts",
                columns: new[] { "user_id", "is_deleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workouts_user_id_is_deleted",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "workouts");

            migrationBuilder.CreateIndex(
                name: "IX_workouts_user_id",
                table: "workouts",
                column: "user_id");
        }
    }
}
