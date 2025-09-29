using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedFieldsToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BlockedUntil",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlockedFromBorrowing",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedUntil",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsBlockedFromBorrowing",
                table: "AspNetUsers");
        }
    }
}
