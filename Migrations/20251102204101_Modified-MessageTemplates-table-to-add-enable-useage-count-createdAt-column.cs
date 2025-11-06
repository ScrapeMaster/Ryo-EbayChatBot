using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbayChatBot.API.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedMessageTemplatestabletoaddenableuseagecountcreatedAtcolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "MessageTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MessageTemplates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "MessageTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "MessageTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MessageTemplates");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "MessageTemplates");
        }
    }
}
