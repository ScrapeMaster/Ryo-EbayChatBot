using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayChatBot.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEbayItemModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PictureUrl",
                table: "EbayItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViewItemUrl",
                table: "EbayItems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PictureUrl",
                table: "EbayItems");

            migrationBuilder.DropColumn(
                name: "ViewItemUrl",
                table: "EbayItems");
        }
    }
}
