using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCase.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RenameInterestRateToRateAmount : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Loan.InterestRate → RateAmount (precision: 5,2 → 7,4)
        migrationBuilder.RenameColumn(
            name: "InterestRate",
            table: "Loans",
            newName: "RateAmount");

        migrationBuilder.AlterColumn<decimal>(
            name: "RateAmount",
            table: "Loans",
            type: "decimal(7,4)",
            precision: 7,
            scale: 4,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(5,2)",
            oldPrecision: 5,
            oldScale: 2);

        // LoanEvaluationResult.ApprovedInterestRate → ApprovedRateAmount
        migrationBuilder.RenameColumn(
            name: "ApprovedInterestRate",
            table: "LoanEvaluations",
            newName: "ApprovedRateAmount");

        migrationBuilder.AlterColumn<decimal>(
            name: "ApprovedRateAmount",
            table: "LoanEvaluations",
            type: "decimal(7,4)",
            precision: 7,
            scale: 4,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(5,2)",
            oldPrecision: 5,
            oldScale: 2);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "RateAmount",
            table: "Loans",
            type: "decimal(5,2)",
            precision: 5,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(7,4)",
            oldPrecision: 7,
            oldScale: 4);

        migrationBuilder.RenameColumn(
            name: "RateAmount",
            table: "Loans",
            newName: "InterestRate");

        migrationBuilder.AlterColumn<decimal>(
            name: "ApprovedRateAmount",
            table: "LoanEvaluations",
            type: "decimal(5,2)",
            precision: 5,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(7,4)",
            oldPrecision: 7,
            oldScale: 4);

        migrationBuilder.RenameColumn(
            name: "ApprovedRateAmount",
            table: "LoanEvaluations",
            newName: "ApprovedInterestRate");
    }
}
