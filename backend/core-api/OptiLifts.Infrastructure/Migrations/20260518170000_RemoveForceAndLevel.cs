using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OptiLifts.Infrastructure.Database;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    [DbContext(typeof(OptiLiftsDbContext))]
    [Migration("20260518170000_RemoveForceAndLevel")]
    public partial class RemoveForceAndLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "force",
                table: "exercises");

            migrationBuilder.DropColumn(
                name: "level",
                table: "exercises");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "force",
                table: "exercises",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "level",
                table: "exercises",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
