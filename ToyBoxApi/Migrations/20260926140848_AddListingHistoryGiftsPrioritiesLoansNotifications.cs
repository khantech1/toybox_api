using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToyBoxApi.Migrations
{
    /// <inheritdoc />
    public partial class AddListingHistoryGiftsPrioritiesLoansNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "current_holder_user_id",
                table: "Toys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_listed",
                table: "Toys",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "Toys",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "due_soon_notified_at",
                table: "Exchange_Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exchange_type",
                table: "Exchange_Requests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "permanent");

            migrationBuilder.AddColumn<DateTime>(
                name: "loan_started_at",
                table: "Exchange_Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "overdue_notified_at",
                table: "Exchange_Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "receiver_user_id",
                table: "Exchange_Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "return_confirmed_by_initiator",
                table: "Exchange_Requests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "return_confirmed_by_receiver",
                table: "Exchange_Requests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "return_due_at",
                table: "Exchange_Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "returned_at",
                table: "Exchange_Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    body = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    toy_id = table.Column<int>(type: "int", nullable: true),
                    request_id = table.Column<int>(type: "int", nullable: true),
                    gift_id = table.Column<int>(type: "int", nullable: true),
                    actor_user_id = table.Column<int>(type: "int", nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Toy_Gifts",
                columns: table => new
                {
                    gift_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    toy_id = table.Column<int>(type: "int", nullable: false),
                    from_user_id = table.Column<int>(type: "int", nullable: false),
                    to_user_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    responded_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toy_Gifts", x => x.gift_id);
                    table.ForeignKey(
                        name: "FK_Toy_Gifts_Toys_toy_id",
                        column: x => x.toy_id,
                        principalTable: "Toys",
                        principalColumn: "toy_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Toy_Gifts_Users_from_user_id",
                        column: x => x.from_user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Toy_Gifts_Users_to_user_id",
                        column: x => x.to_user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Toy_Ownership_History",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    toy_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    request_id = table.Column<int>(type: "int", nullable: true),
                    gift_id = table.Column<int>(type: "int", nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ended_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toy_Ownership_History", x => x.history_id);
                    table.ForeignKey(
                        name: "FK_Toy_Ownership_History_Toys_toy_id",
                        column: x => x.toy_id,
                        principalTable: "Toys",
                        principalColumn: "toy_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Toy_Ownership_History_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Toy_Priorities",
                columns: table => new
                {
                    toy_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toy_Priorities", x => new { x.toy_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_Toy_Priorities_Toys_toy_id",
                        column: x => x.toy_id,
                        principalTable: "Toys",
                        principalColumn: "toy_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Toy_Priorities_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Toys_current_holder_user_id",
                table: "Toys",
                column: "current_holder_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Exchange_Requests_receiver_user_id",
                table: "Exchange_Requests",
                column: "receiver_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_user_id_is_read_created_at",
                table: "Notifications",
                columns: new[] { "user_id", "is_read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Toy_Gifts_from_user_id",
                table: "Toy_Gifts",
                column: "from_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Toy_Gifts_to_user_id_status",
                table: "Toy_Gifts",
                columns: new[] { "to_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Toy_Gifts_toy_id",
                table: "Toy_Gifts",
                column: "toy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Toy_Ownership_History_toy_id_ended_at",
                table: "Toy_Ownership_History",
                columns: new[] { "toy_id", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Toy_Ownership_History_user_id",
                table: "Toy_Ownership_History",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Toy_Priorities_user_id",
                table: "Toy_Priorities",
                column: "user_id");

            // ── Backfill existing data (must run before the new FKs are added) ──
            migrationBuilder.Sql(
                "UPDATE Toys SET current_holder_user_id = owner_user_id;");

            migrationBuilder.Sql(@"
                INSERT INTO Toy_Ownership_History (toy_id, user_id, type, started_at, ended_at)
                SELECT toy_id, owner_user_id, 'created', SYSUTCDATETIME(), NULL FROM Toys;");

            // Receiver = owner of the requested toys. For completed exchanges ownership
            // was already swapped, so the receiver now owns the offered toys.
            migrationBuilder.Sql(@"
                UPDATE r SET receiver_user_id = (
                    SELECT TOP 1 t.owner_user_id
                    FROM Exchange_Request_Toys rt
                    JOIN Toys t ON t.toy_id = rt.toy_id
                    WHERE rt.request_id = r.request_id
                      AND rt.exchange_role = CASE WHEN r.status = 'completed' THEN 'offered' ELSE 'requested' END)
                FROM Exchange_Requests r;");

            // Retroactive bug fix: toys received through a completed exchange were
            // auto-listed; they belong in the new owner's toys, unlisted.
            migrationBuilder.Sql(@"
                UPDATE Toys SET is_listed = 0
                WHERE toy_id IN (
                    SELECT rt.toy_id
                    FROM Exchange_Request_Toys rt
                    JOIN Exchange_Requests r ON r.request_id = rt.request_id
                    WHERE r.status = 'completed');");

            migrationBuilder.AddForeignKey(
                name: "FK_Exchange_Requests_Users_receiver_user_id",
                table: "Exchange_Requests",
                column: "receiver_user_id",
                principalTable: "Users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Toys_Users_current_holder_user_id",
                table: "Toys",
                column: "current_holder_user_id",
                principalTable: "Users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exchange_Requests_Users_receiver_user_id",
                table: "Exchange_Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Toys_Users_current_holder_user_id",
                table: "Toys");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Toy_Gifts");

            migrationBuilder.DropTable(
                name: "Toy_Ownership_History");

            migrationBuilder.DropTable(
                name: "Toy_Priorities");

            migrationBuilder.DropIndex(
                name: "IX_Toys_current_holder_user_id",
                table: "Toys");

            migrationBuilder.DropIndex(
                name: "IX_Exchange_Requests_receiver_user_id",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "current_holder_user_id",
                table: "Toys");

            migrationBuilder.DropColumn(
                name: "is_listed",
                table: "Toys");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "Toys");

            migrationBuilder.DropColumn(
                name: "due_soon_notified_at",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "exchange_type",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "loan_started_at",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "overdue_notified_at",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "receiver_user_id",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "return_confirmed_by_initiator",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "return_confirmed_by_receiver",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "return_due_at",
                table: "Exchange_Requests");

            migrationBuilder.DropColumn(
                name: "returned_at",
                table: "Exchange_Requests");
        }
    }
}
