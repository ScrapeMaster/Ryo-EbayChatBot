using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayChatBot.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEbayMessageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ItemId",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "ChatMessages");
        }
    }
}
