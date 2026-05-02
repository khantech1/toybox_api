using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ToyBoxApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    category_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    profile_pic = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    rating = table.Column<decimal>(type: "decimal(4,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    contact_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => new { x.user_id, x.contact_id });
                    table.ForeignKey(
                        name: "FK_Contacts_Users_contact_id",
                        column: x => x.contact_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contacts_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Exchange_Requests",
                columns: table => new
                {
                    request_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    initiator_user_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exchange_Requests", x => x.request_id);
                    table.ForeignKey(
                        name: "FK_Exchange_Requests_Users_initiator_user_id",
                        column: x => x.initiator_user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Toys",
                columns: table => new
                {
                    toy_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    owner_user_id = table.Column<int>(type: "int", nullable: false),
                    toy_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    toy_description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    category_id = table.Column<int>(type: "int", nullable: true),
                    desired_category_id = table.Column<int>(type: "int", nullable: true),
                    condition_status = table.Column<int>(type: "int", nullable: true),
                    value = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toys", x => x.toy_id);
                    table.ForeignKey(
                        name: "FK_Toys_Categories_category_id",
                        column: x => x.category_id,
                        principalTable: "Categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Toys_Categories_desired_category_id",
                        column: x => x.desired_category_id,
                        principalTable: "Categories",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "FK_Toys_Users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    request_id = table.Column<int>(type: "int", nullable: false),
                    reviewer_user_id = table.Column<int>(type: "int", nullable: false),
                    reviewee_user_id = table.Column<int>(type: "int", nullable: false),
                    rating_score = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.review_id);
                    table.ForeignKey(
                        name: "FK_Reviews_Exchange_Requests_request_id",
                        column: x => x.request_id,
                        principalTable: "Exchange_Requests",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_reviewee_user_id",
                        column: x => x.reviewee_user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_reviewer_user_id",
                        column: x => x.reviewer_user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Exchange_Request_Toys",
                columns: table => new
                {
                    request_id = table.Column<int>(type: "int", nullable: false),
                    toy_id = table.Column<int>(type: "int", nullable: false),
                    exchange_role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exchange_Request_Toys", x => new { x.request_id, x.toy_id });
                    table.ForeignKey(
                        name: "FK_Exchange_Request_Toys_Exchange_Requests_request_id",
                        column: x => x.request_id,
                        principalTable: "Exchange_Requests",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exchange_Request_Toys_Toys_toy_id",
                        column: x => x.toy_id,
                        principalTable: "Toys",
                        principalColumn: "toy_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shared_Toys",
                columns: table => new
                {
                    shared_with_user_id = table.Column<int>(type: "int", nullable: false),
                    toy_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shared_Toys", x => new { x.shared_with_user_id, x.toy_id });
                    table.ForeignKey(
                        name: "FK_Shared_Toys_Toys_toy_id",
                        column: x => x.toy_id,
                        principalTable: "Toys",
                        principalColumn: "toy_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shared_Toys_Users_shared_with_user_id",
                        column: x => x.shared_with_user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Toy_Images",
                columns: table => new
                {
                    image_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    toy_id = table.Column<int>(type: "int", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Toy_Images", x => x.image_id);
                    table.ForeignKey(
                        name: "FK_Toy_Images_Toys_toy_id",
                        column: x => x.toy_id,
                        principalTable: "Toys",
                        principalColumn: "toy_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "category_id", "category_name" },
                values: new object[,]
                {
                    { 1, "Educational" },
                    { 2, "Creative" },
                    { 3, "Vehicles" },
                    { 4, "Dolls & Figures" },
                    { 5, "Outdoor & Sports" },
                    { 6, "Puzzles & Games" },
                    { 7, "Building Blocks" },
                    { 8, "Electronic Toys" },
                    { 9, "Stuffed Animals" },
                    { 10, "Arts & Crafts" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_contact_id",
                table: "Contacts",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_Exchange_Request_Toys_toy_id",
                table: "Exchange_Request_Toys",
                column: "toy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Exchange_Requests_initiator_user_id",
                table: "Exchange_Requests",
                column: "initiator_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_request_id_reviewer_user_id",
                table: "Reviews",
                columns: new[] { "request_id", "reviewer_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_reviewee_user_id",
                table: "Reviews",
                column: "reviewee_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_reviewer_user_id",
                table: "Reviews",
                column: "reviewer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Shared_Toys_toy_id",
                table: "Shared_Toys",
                column: "toy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Toy_Images_toy_id",
                table: "Toy_Images",
                column: "toy_id");

            migrationBuilder.CreateIndex(
                name: "IX_Toys_category_id",
                table: "Toys",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Toys_desired_category_id",
                table: "Toys",
                column: "desired_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_Toys_owner_user_id",
                table: "Toys",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "Exchange_Request_Toys");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Shared_Toys");

            migrationBuilder.DropTable(
                name: "Toy_Images");

            migrationBuilder.DropTable(
                name: "Exchange_Requests");

            migrationBuilder.DropTable(
                name: "Toys");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
