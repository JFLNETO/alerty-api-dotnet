using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Alerty.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertasEPlano : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "plano",
                schema: "alerty",
                table: "config_empresa",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "historico_notificacoes",
                schema: "alerty",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_regra_alerta = table.Column<int>(type: "integer", nullable: false),
                    data_vencimento_referencia = table.Column<DateOnly>(type: "date", nullable: false),
                    data_envio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    erro_mensagem = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_notificacoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "regras_alerta",
                schema: "alerty",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    dias_offset = table.Column<int>(type: "integer", nullable: false),
                    mensagem = table.Column<string>(type: "text", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regras_alerta", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historico_notificacoes",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "regras_alerta",
                schema: "alerty");

            migrationBuilder.DropColumn(
                name: "plano",
                schema: "alerty",
                table: "config_empresa");
        }
    }
}
