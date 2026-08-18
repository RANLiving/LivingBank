using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivingBank.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Utilizadores existentes já têm password real definida — só os novos
            // (criados via convite) devem começar como false, definido explicitamente no código.
            migrationBuilder.AddColumn<bool>(
                name: "PasswordSet",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordSet",
                table: "AspNetUsers");
        }
    }
}
