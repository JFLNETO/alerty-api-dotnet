using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alerty.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWahaSessionPorEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "waha_session",
                schema: "alerty",
                table: "config_empresa",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "waha_session",
                schema: "alerty",
                table: "config_empresa");
        }
    }
}
