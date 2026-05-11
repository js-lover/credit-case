using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditScoreBonusToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditScoreBonus",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditScoreBonus",
                table: "Customers");
        }
    }
}
