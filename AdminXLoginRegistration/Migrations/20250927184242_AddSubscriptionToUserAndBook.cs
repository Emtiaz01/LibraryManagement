using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionToUserAndBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Product",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscribed",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionEndDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "IsSubscribed",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SubscriptionEndDate",
                table: "AspNetUsers");
        }
    }
}
