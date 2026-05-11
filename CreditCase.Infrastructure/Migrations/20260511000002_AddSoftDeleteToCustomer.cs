using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCase.Infrastructure.Migrations;

public partial class AddSoftDeleteToCustomer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            table: "Customers",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeletedAt",
            table: "Customers",
            type: "datetime2",
            nullable: true);

        // Eski unique index'leri kaldır (filtre desteklemiyor)
        migrationBuilder.DropIndex(name: "IX_Customers_Email", table: "Customers");
        migrationBuilder.DropIndex(name: "IX_Customers_IdentityNumber", table: "Customers");

        // Filtered unique index'ler: sadece IsDeleted=0 olan kayıtlar için benzersizlik
        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX [IX_Customers_Email] ON [Customers] ([Email]) WHERE [IsDeleted] = 0;");
        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX [IX_Customers_IdentityNumber] ON [Customers] ([IdentityNumber]) WHERE [IsDeleted] = 0;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Customers_Email", table: "Customers");
        migrationBuilder.DropIndex(name: "IX_Customers_IdentityNumber", table: "Customers");

        migrationBuilder.DropColumn(name: "IsDeleted", table: "Customers");
        migrationBuilder.DropColumn(name: "DeletedAt", table: "Customers");

        migrationBuilder.CreateIndex(name: "IX_Customers_Email", table: "Customers", column: "Email", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Customers_IdentityNumber", table: "Customers", column: "IdentityNumber", unique: true);
    }
}
