using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alerty.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAdminUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_admin",
                schema: "alerty",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_admin",
                schema: "alerty",
                table: "usuarios");
        }
    }
}
