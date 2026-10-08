using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToyBoxApi.Migrations
{
    /// <inheritdoc />
    public partial class AddChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "child_id",
                table: "Toy_Gifts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "child_name",
                table: "Toy_Gifts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    child_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_user_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    interests = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    is_visible_to_contacts = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.child_id);
                    table.ForeignKey(
                        name: "FK_Children_Users_parent_user_id",
                        column: x => x.parent_user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Children_parent_user_id",
                table: "Children",
                column: "parent_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Children");

            migrationBuilder.DropColumn(
                name: "child_id",
                table: "Toy_Gifts");

            migrationBuilder.DropColumn(
                name: "child_name",
                table: "Toy_Gifts");
        }
    }
}
