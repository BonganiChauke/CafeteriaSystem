using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeteriaSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionTypeToDepositHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyDepositTotal",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyDepositTotal",
                table: "DepositHistories",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "DepositHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyDepositTotal",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MonthlyDepositTotal",
                table: "DepositHistories");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "DepositHistories");
        }
    }
}
