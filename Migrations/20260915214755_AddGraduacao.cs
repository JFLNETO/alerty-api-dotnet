using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Alerty.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGraduacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cliente_graduacoes",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_servico = table.Column<int>(type: "integer", nullable: false),
                    id_graduacao_atual = table.Column<int>(type: "integer", nullable: false),
                    data_ultima_graduacao = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente_graduacoes", x => x.id_unico);
                });

            migrationBuilder.CreateTable(
                name: "graduacoes",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    id_servico = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    requisito_dias = table.Column<int>(type: "integer", nullable: false),
                    requisito_presencas = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_graduacoes", x => x.id_unico);
                });

            migrationBuilder.CreateTable(
                name: "historico_graduacoes",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_servico = table.Column<int>(type: "integer", nullable: false),
                    id_graduacao_anterior = table.Column<int>(type: "integer", nullable: false),
                    id_graduacao_nova = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmado_por = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_graduacoes", x => x.id_unico);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cliente_graduacoes_id_cliente_id_servico",
                schema: "alerty",
                table: "cliente_graduacoes",
                columns: new[] { "id_cliente", "id_servico" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cliente_graduacoes",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "graduacoes",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "historico_graduacoes",
                schema: "alerty");
        }
    }
}
