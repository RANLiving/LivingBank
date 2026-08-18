using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivingBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordChangedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordChangedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "AspNetUsers");
        }
    }
}
