using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayChatBot.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEbayUsernameToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EbayUsername",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EbayUsername",
                table: "Users");
        }
    }
}
