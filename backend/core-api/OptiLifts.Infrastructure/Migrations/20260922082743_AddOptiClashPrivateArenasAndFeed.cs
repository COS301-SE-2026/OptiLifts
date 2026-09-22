using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptiClashPrivateArenasAndFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "arena_invites",
                columns: table => new
                {
                    invite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    arena_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arena_invites", x => x.invite_id);
                    table.ForeignKey(
                        name: "FK_arena_invites_arenas_arena_id",
                        column: x => x.arena_id,
                        principalTable: "arenas",
                        principalColumn: "arena_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_arena_invites_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_arena_invites_users_invited_user_id",
                        column: x => x.invited_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "arena_members",
                columns: table => new
                {
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    arena_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Member"),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_arena_members", x => x.member_id);
                    table.ForeignKey(
                        name: "FK_arena_members_arenas_arena_id",
                        column: x => x.arena_id,
                        principalTable: "arenas",
                        principalColumn: "arena_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_arena_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clash_activities",
                columns: table => new
                {
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    arena_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_text = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    details = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_pr = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_promotion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    kudos_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clash_activities", x => x.activity_id);
                    table.ForeignKey(
                        name: "FK_clash_activities_arenas_arena_id",
                        column: x => x.arena_id,
                        principalTable: "arenas",
                        principalColumn: "arena_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clash_activities_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "clash_activity_kudos",
                columns: table => new
                {
                    kudos_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clash_activity_kudos", x => x.kudos_id);
                    table.ForeignKey(
                        name: "FK_clash_activity_kudos_clash_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "clash_activities",
                        principalColumn: "activity_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clash_activity_kudos_users_sender_user_id",
                        column: x => x.sender_user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_arena_invites_arena_id",
                table: "arena_invites",
                column: "arena_id");

            migrationBuilder.CreateIndex(
                name: "IX_arena_invites_invited_by_user_id",
                table: "arena_invites",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_arena_invites_invited_user_id_status",
                table: "arena_invites",
                columns: new[] { "invited_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_arena_members_arena_id_user_id",
                table: "arena_members",
                columns: new[] { "arena_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_arena_members_user_id",
                table: "arena_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_clash_activities_arena_id_created_at",
                table: "clash_activities",
                columns: new[] { "arena_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_clash_activities_user_id",
                table: "clash_activities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_clash_activity_kudos_activity_id_sender_user_id",
                table: "clash_activity_kudos",
                columns: new[] { "activity_id", "sender_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clash_activity_kudos_sender_user_id",
                table: "clash_activity_kudos",
                column: "sender_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "arena_invites");

            migrationBuilder.DropTable(
                name: "arena_members");

            migrationBuilder.DropTable(
                name: "clash_activity_kudos");

            migrationBuilder.DropTable(
                name: "clash_activities");
        }
    }
}
