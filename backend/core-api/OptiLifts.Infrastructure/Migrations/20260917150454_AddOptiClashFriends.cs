using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptiClashFriends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "duel_invite_privacy",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Friends");

            migrationBuilder.AddColumn<string>(
                name: "friend_code",
                table: "users",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "global_leaderboard_opt_in",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "friend_requests",
                columns: table => new
                {
                    friend_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend_requests", x => x.friend_request_id);
                    table.ForeignKey(
                        name: "FK_friend_requests_users_receiver_id",
                        column: x => x.receiver_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friend_requests_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "friendships",
                columns: table => new
                {
                    friendship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id1 = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id2 = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friendships", x => x.friendship_id);
                    table.CheckConstraint("CK_friendships_user_order", "user_id1 < user_id2");
                    table.ForeignKey(
                        name: "FK_friendships_users_user_id1",
                        column: x => x.user_id1,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friendships_users_user_id2",
                        column: x => x.user_id2,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            //backfill existing users
            migrationBuilder.Sql(@"UPDATE users SET friend_code = 'CS6499' 
                WHERE email_hash = encode(sha256('test@optilifts.com'::bytea), 'hex')
                AND (friend_code IS NULL OR friend_code = '');
                UPDATE users SET friend_code = UPPER(SUBSTRING(MD5(RANDOM()::text || user_id::text) FROM 1 FOR 6))
                WHERE friend_code IS NULL OR friend_code = '';
                ");

            migrationBuilder.CreateIndex(
                name: "IX_users_friend_code",
                table: "users",
                column: "friend_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_friend_requests_receiver_id_status",
                table: "friend_requests",
                columns: new[] { "receiver_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_friend_requests_sender_id_receiver_id",
                table: "friend_requests",
                columns: new[] { "sender_id", "receiver_id" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_friendships_user_id1_user_id2",
                table: "friendships",
                columns: new[] { "user_id1", "user_id2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_friendships_user_id2",
                table: "friendships",
                column: "user_id2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "friend_requests");

            migrationBuilder.DropTable(
                name: "friendships");

            migrationBuilder.DropIndex(
                name: "IX_users_friend_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "duel_invite_privacy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "friend_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "global_leaderboard_opt_in",
                table: "users");
        }
    }
}
