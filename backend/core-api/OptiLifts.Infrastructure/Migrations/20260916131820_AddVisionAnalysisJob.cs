using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisionAnalysisJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vision_analysis_jobs",
                columns: table => new
                {
                    job_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    exercise = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    view = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    blob_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    detected_anomalies = table.Column<List<string>>(type: "text[]", nullable: false),
                    coach_summary = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vision_analysis_jobs", x => x.job_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vision_analysis_jobs_status",
                table: "vision_analysis_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_vision_analysis_jobs_user_id",
                table: "vision_analysis_jobs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_vision_analysis_jobs_user_id_created_at",
                table: "vision_analysis_jobs",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vision_analysis_jobs");
        }
    }
}
