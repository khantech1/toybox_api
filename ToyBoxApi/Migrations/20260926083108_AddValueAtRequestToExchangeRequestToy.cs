using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToyBoxApi.Migrations
{
    /// <inheritdoc />
    public partial class AddValueAtRequestToExchangeRequestToy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "value_at_request",
                table: "Exchange_Request_Toys",
                type: "decimal(10,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "value_at_request",
                table: "Exchange_Request_Toys");
        }
    }
}
