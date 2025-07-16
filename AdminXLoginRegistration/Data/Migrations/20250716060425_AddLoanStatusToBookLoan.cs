using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminXLoginRegistration.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanStatusToBookLoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BookLoan",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "BookLoan");
        }
    }
}
