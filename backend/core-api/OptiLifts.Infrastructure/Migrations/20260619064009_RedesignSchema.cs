using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptiLifts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sets_exercises_exercise_id",
                table: "sets");

            migrationBuilder.DropForeignKey(
                name: "FK_sets_workouts_workout_id",
                table: "sets");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_log_sets_exercises_exercise_id",
                table: "workout_log_sets");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_logs_users_user_id",
                table: "workout_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_logs_workouts_workout_id",
                table: "workout_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_workouts_users_created_by",
                table: "workouts");

            migrationBuilder.DropIndex(
                name: "IX_workout_logs_user_id",
                table: "workout_logs");

            migrationBuilder.DropIndex(
                name: "IX_workout_logs_workout_id",
                table: "workout_logs");

            migrationBuilder.DropIndex(
                name: "IX_sets_exercise_id",
                table: "sets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exercises",
                table: "exercises");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "workout_logs");

            migrationBuilder.DropColumn(
                name: "workout_id",
                table: "workout_logs");

            migrationBuilder.DropColumn(
                name: "exercise_id",
                table: "sets");

            migrationBuilder.DropColumn(
                name: "primary_muscles",
                table: "exercises");

            migrationBuilder.DropColumn(
                name: "secondary_muscles",
                table: "exercises");

            migrationBuilder.RenameTable(
                name: "exercises",
                newName: "exercise_dictionary");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "workouts",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_workouts_created_by",
                table: "workouts",
                newName: "IX_workouts_user_id");

            migrationBuilder.RenameColumn(
                name: "workout_id",
                table: "sets",
                newName: "workout_exercise_id");

            migrationBuilder.RenameIndex(
                name: "IX_sets_workout_id",
                table: "sets",
                newName: "IX_sets_workout_exercise_id");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "exercise_dictionary",
                newName: "image_url");

            migrationBuilder.RenameColumn(
                name: "exercise_id",
                table: "exercise_dictionary",
                newName: "exercise_dict_id");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "exercise_dictionary",
                newName: "exercise_type");

            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "height",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "level",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "light_theme",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "metric",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "refresh_token_expiry",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "refresh_token_hash",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "weight",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "weight",
                table: "sets",
                type: "real",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<int>(
                name: "reps",
                table: "sets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<float>(
                name: "distance",
                table: "sets",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration",
                table: "sets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "primary_muscle",
                table: "exercise_dictionary",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_exercise_dictionary",
                table: "exercise_dictionary",
                column: "exercise_dict_id");

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    send_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.message_id);
                    table.ForeignKey(
                        name: "FK_messages_users_receiver_id",
                        column: x => x.receiver_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_messages_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "muscles",
                columns: table => new
                {
                    muscle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_muscles", x => x.muscle_id);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_entries",
                columns: table => new
                {
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_entries", x => x.entry_id);
                    table.ForeignKey(
                        name: "FK_scheduled_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scheduled_entries_workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_models",
                columns: table => new
                {
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_path = table.Column<string>(type: "text", nullable: false),
                    training_sessions = table.Column<int>(type: "integer", nullable: false),
                    trained_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_models", x => x.model_id);
                    table.ForeignKey(
                        name: "FK_user_models_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_exercises",
                columns: table => new
                {
                    workout_exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_dict_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_exercises", x => x.workout_exercise_id);
                    table.ForeignKey(
                        name: "FK_workout_exercises_exercise_dictionary_exercise_dict_id",
                        column: x => x.exercise_dict_id,
                        principalTable: "exercise_dictionary",
                        principalColumn: "exercise_dict_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_exercises_workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sec_muscles",
                columns: table => new
                {
                    sec_muscle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    muscle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sec_muscles", x => x.sec_muscle_id);
                    table.ForeignKey(
                        name: "FK_sec_muscles_exercise_dictionary_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercise_dictionary",
                        principalColumn: "exercise_dict_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sec_muscles_muscles_muscle_id",
                        column: x => x.muscle_id,
                        principalTable: "muscles",
                        principalColumn: "muscle_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workout_logs_entry_id",
                table: "workout_logs",
                column: "entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_dictionary_primary_muscle",
                table: "exercise_dictionary",
                column: "primary_muscle");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_dictionary_user_id",
                table: "exercise_dictionary",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_receiver_id",
                table: "messages",
                column: "receiver_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_sender_id",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entries_user_id",
                table: "scheduled_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_entries_workout_id",
                table: "scheduled_entries",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "IX_sec_muscles_exercise_id",
                table: "sec_muscles",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_sec_muscles_muscle_id",
                table: "sec_muscles",
                column: "muscle_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_models_user_id",
                table: "user_models",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_exercise_dict_id",
                table: "workout_exercises",
                column: "exercise_dict_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_workout_id",
                table: "workout_exercises",
                column: "workout_id");

            migrationBuilder.AddForeignKey(
                name: "FK_exercise_dictionary_muscles_primary_muscle",
                table: "exercise_dictionary",
                column: "primary_muscle",
                principalTable: "muscles",
                principalColumn: "muscle_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_exercise_dictionary_users_user_id",
                table: "exercise_dictionary",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sets_workout_exercises_workout_exercise_id",
                table: "sets",
                column: "workout_exercise_id",
                principalTable: "workout_exercises",
                principalColumn: "workout_exercise_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_log_sets_exercise_dictionary_exercise_id",
                table: "workout_log_sets",
                column: "exercise_id",
                principalTable: "exercise_dictionary",
                principalColumn: "exercise_dict_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_logs_scheduled_entries_entry_id",
                table: "workout_logs",
                column: "entry_id",
                principalTable: "scheduled_entries",
                principalColumn: "entry_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_workouts_users_user_id",
                table: "workouts",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exercise_dictionary_muscles_primary_muscle",
                table: "exercise_dictionary");

            migrationBuilder.DropForeignKey(
                name: "FK_exercise_dictionary_users_user_id",
                table: "exercise_dictionary");

            migrationBuilder.DropForeignKey(
                name: "FK_sets_workout_exercises_workout_exercise_id",
                table: "sets");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_log_sets_exercise_dictionary_exercise_id",
                table: "workout_log_sets");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_logs_scheduled_entries_entry_id",
                table: "workout_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_workouts_users_user_id",
                table: "workouts");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "scheduled_entries");

            migrationBuilder.DropTable(
                name: "sec_muscles");

            migrationBuilder.DropTable(
                name: "user_models");

            migrationBuilder.DropTable(
                name: "workout_exercises");

            migrationBuilder.DropTable(
                name: "muscles");

            migrationBuilder.DropIndex(
                name: "IX_workout_logs_entry_id",
                table: "workout_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exercise_dictionary",
                table: "exercise_dictionary");

            migrationBuilder.DropIndex(
                name: "IX_exercise_dictionary_primary_muscle",
                table: "exercise_dictionary");

            migrationBuilder.DropIndex(
                name: "IX_exercise_dictionary_user_id",
                table: "exercise_dictionary");

            migrationBuilder.DropColumn(
                name: "height",
                table: "users");

            migrationBuilder.DropColumn(
                name: "level",
                table: "users");

            migrationBuilder.DropColumn(
                name: "light_theme",
                table: "users");

            migrationBuilder.DropColumn(
                name: "metric",
                table: "users");

            migrationBuilder.DropColumn(
                name: "refresh_token_expiry",
                table: "users");

            migrationBuilder.DropColumn(
                name: "refresh_token_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "weight",
                table: "users");

            migrationBuilder.DropColumn(
                name: "distance",
                table: "sets");

            migrationBuilder.DropColumn(
                name: "duration",
                table: "sets");

            migrationBuilder.DropColumn(
                name: "primary_muscle",
                table: "exercise_dictionary");

            migrationBuilder.RenameTable(
                name: "exercise_dictionary",
                newName: "exercises");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "workouts",
                newName: "created_by");

            migrationBuilder.RenameIndex(
                name: "IX_workouts_user_id",
                table: "workouts",
                newName: "IX_workouts_created_by");

            migrationBuilder.RenameColumn(
                name: "workout_exercise_id",
                table: "sets",
                newName: "workout_id");

            migrationBuilder.RenameIndex(
                name: "IX_sets_workout_exercise_id",
                table: "sets",
                newName: "IX_sets_workout_id");

            migrationBuilder.RenameColumn(
                name: "image_url",
                table: "exercises",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "exercise_dict_id",
                table: "exercises",
                newName: "exercise_id");

            migrationBuilder.RenameColumn(
                name: "exercise_type",
                table: "exercises",
                newName: "category");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "workout_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "workout_id",
                table: "workout_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<float>(
                name: "weight",
                table: "sets",
                type: "real",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "reps",
                table: "sets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "exercise_id",
                table: "sets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<List<string>>(
                name: "primary_muscles",
                table: "exercises",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "secondary_muscles",
                table: "exercises",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_exercises",
                table: "exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_logs_user_id",
                table: "workout_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_logs_workout_id",
                table: "workout_logs",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "IX_sets_exercise_id",
                table: "sets",
                column: "exercise_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sets_exercises_exercise_id",
                table: "sets",
                column: "exercise_id",
                principalTable: "exercises",
                principalColumn: "exercise_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sets_workouts_workout_id",
                table: "sets",
                column: "workout_id",
                principalTable: "workouts",
                principalColumn: "workout_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_log_sets_exercises_exercise_id",
                table: "workout_log_sets",
                column: "exercise_id",
                principalTable: "exercises",
                principalColumn: "exercise_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_logs_users_user_id",
                table: "workout_logs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_logs_workouts_workout_id",
                table: "workout_logs",
                column: "workout_id",
                principalTable: "workouts",
                principalColumn: "workout_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_workouts_users_created_by",
                table: "workouts",
                column: "created_by",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
