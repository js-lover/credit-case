using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCase.Infrastructure.Migrations;

public partial class AddIsBalloonToInstallment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsBalloon",
            table: "Installments",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsBalloon", table: "Installments");
    }
}
