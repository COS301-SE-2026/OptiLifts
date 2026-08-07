using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeleteAccountChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_users_receiver_id",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_logs_scheduled_entries_entry_id",
                table: "workout_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_workouts_folders_folder_id",
                table: "workouts");

            migrationBuilder.DropForeignKey(
                name: "FK_workouts_users_user_id",
                table: "workouts");

            migrationBuilder.AddForeignKey(
                name: "FK_messages_users_receiver_id",
                table: "messages",
                column: "receiver_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_logs_scheduled_entries_entry_id",
                table: "workout_logs",
                column: "entry_id",
                principalTable: "scheduled_entries",
                principalColumn: "entry_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workouts_folders_folder_id",
                table: "workouts",
                column: "folder_id",
                principalTable: "folders",
                principalColumn: "folder_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_workouts_users_user_id",
                table: "workouts",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_users_receiver_id",
                table: "messages");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_logs_scheduled_entries_entry_id",
                table: "workout_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_workouts_folders_folder_id",
                table: "workouts");

            migrationBuilder.DropForeignKey(
                name: "FK_workouts_users_user_id",
                table: "workouts");

            migrationBuilder.AddForeignKey(
                name: "FK_messages_users_receiver_id",
                table: "messages",
                column: "receiver_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_logs_scheduled_entries_entry_id",
                table: "workout_logs",
                column: "entry_id",
                principalTable: "scheduled_entries",
                principalColumn: "entry_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_workouts_folders_folder_id",
                table: "workouts",
                column: "folder_id",
                principalTable: "folders",
                principalColumn: "folder_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workouts_users_user_id",
                table: "workouts",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
