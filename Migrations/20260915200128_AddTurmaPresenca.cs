using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Alerty.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTurmaPresenca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "id_turmas",
                schema: "alerty",
                table: "clientes",
                type: "integer[]",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "presencas",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_turma = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    lancado_por = table.Column<int>(type: "integer", nullable: false),
                    data_hora_lancamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presencas", x => x.id_unico);
                });

            migrationBuilder.CreateTable(
                name: "turmas",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    id_servico = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: true),
                    dia_semana = table.Column<int>(type: "integer", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turmas", x => x.id_unico);
                });

            migrationBuilder.CreateIndex(
                name: "IX_presencas_id_turma_data_id_cliente",
                schema: "alerty",
                table: "presencas",
                columns: new[] { "id_turma", "data", "id_cliente" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "presencas",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "turmas",
                schema: "alerty");

            migrationBuilder.DropColumn(
                name: "id_turmas",
                schema: "alerty",
                table: "clientes");
        }
    }
}
