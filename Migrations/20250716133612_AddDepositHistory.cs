using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeteriaSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDepositHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Employees_UserId",
                table: "Employees",
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "DepositHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepositDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepositHistories_EmployeeId",
                table: "DepositHistories",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepositHistories");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Employees_UserId",
                table: "Employees");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);
        }
    }
}
