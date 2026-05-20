using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OptiLifts.Infrastructure.Database;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    [DbContext(typeof(OptiLiftsDbContext))]
    [Migration("20260519120000_AddExerciseUserId")]
    public partial class AddExerciseUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "exercises",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercises_user_id",
                table: "exercises",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exercises_user_id",
                table: "exercises");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "exercises");
        }
    }
}