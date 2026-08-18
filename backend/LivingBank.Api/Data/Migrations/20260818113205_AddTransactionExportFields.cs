using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivingBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionExportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExportedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExported",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExportedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsExported",
                table: "Transactions");
        }
    }
}
