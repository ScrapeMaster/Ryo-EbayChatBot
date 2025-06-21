using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayChatBot.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLatestChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalMessageId",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessageDirection",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SenderEbayUsername",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalMessageId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "MessageDirection",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "SenderEbayUsername",
                table: "ChatMessages");
        }
    }
}
