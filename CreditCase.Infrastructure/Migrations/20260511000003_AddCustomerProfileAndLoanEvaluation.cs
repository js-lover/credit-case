using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCase.Infrastructure.Migrations;

public partial class AddCustomerProfileAndLoanEvaluation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Customer profil alanları ─────────────────────────────────────────────
        migrationBuilder.AddColumn<DateTime>(
            name: "DateOfBirth",
            table: "Customers",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        migrationBuilder.AddColumn<decimal>(
            name: "MonthlyIncome",
            table: "Customers",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "ProfessionCategory",
            table: "Customers",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "Other");

        migrationBuilder.AddColumn<string>(
            name: "EmploymentStatus",
            table: "Customers",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "Active");

        // ── LoanEvaluations tablosu ──────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "LoanEvaluations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                RequestedTerm = table.Column<int>(type: "int", nullable: false),
                RequestedLoanType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsApproved = table.Column<bool>(type: "bit", nullable: false),
                ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                MaximumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                MaximumTerm = table.Column<int>(type: "int", nullable: false),
                ApprovedInterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreditScore = table.Column<int>(type: "int", nullable: false),
                DebtToIncomeRatio = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                MonthlyInstallmentEstimate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                EvaluationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoanEvaluations", x => x.Id);
                table.ForeignKey(
                    name: "FK_LoanEvaluations_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LoanEvaluations_CustomerId",
            table: "LoanEvaluations",
            column: "CustomerId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LoanEvaluations");

        migrationBuilder.DropColumn(name: "DateOfBirth", table: "Customers");
        migrationBuilder.DropColumn(name: "MonthlyIncome", table: "Customers");
        migrationBuilder.DropColumn(name: "ProfessionCategory", table: "Customers");
        migrationBuilder.DropColumn(name: "EmploymentStatus", table: "Customers");
    }
}
