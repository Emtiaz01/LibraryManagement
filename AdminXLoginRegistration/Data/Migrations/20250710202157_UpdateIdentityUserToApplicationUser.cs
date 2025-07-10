using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminXLoginRegistration.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIdentityUserToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<int>(
            //    name: "ProductQuantity",
            //    table: "Product",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductQuantity",
                table: "Product");
        }
    }
}
