using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alerty.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTelefoneAvisoInvalidoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telefone_aviso_invalido",
                schema: "alerty",
                table: "clientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "telefone_aviso_invalido",
                schema: "alerty",
                table: "clientes");
        }
    }
}
