using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Alerty.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "alerty");

            migrationBuilder.CreateTable(
                name: "clientes",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "text", nullable: true),
                    id_cliente = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    data_ultimo_pagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_servicos = table.Column<int[]>(type: "integer[]", nullable: true),
                    selos = table.Column<int[]>(type: "integer[]", nullable: true),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    url_foto = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id_unico);
                });

            migrationBuilder.CreateTable(
                name: "config_empresa",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "text", nullable: true),
                    nome_dono = table.Column<string>(type: "text", nullable: true),
                    whatsapp_dono = table.Column<string>(type: "text", nullable: true),
                    limite_notificacoes = table.Column<int>(type: "integer", nullable: true),
                    id_empresa = table.Column<int>(type: "integer", nullable: true),
                    selos = table.Column<int[]>(type: "integer[]", nullable: true),
                    link_logo = table.Column<string>(type: "text", nullable: true),
                    data_vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_empresa", x => x.id_unico);
                });

            migrationBuilder.CreateTable(
                name: "historico_cobranca",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cliente = table.Column<string>(type: "text", nullable: true),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: false),
                    data_pagamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_cobranca", x => x.id_unico);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "alerty",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(type: "text", nullable: false),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "servicos",
                schema: "alerty",
                columns: table => new
                {
                    id_unico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "text", nullable: true),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric", nullable: true),
                    recorrencia_valor = table.Column<int>(type: "integer", nullable: true),
                    recorrencia_tipo = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicos", x => x.id_unico);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "alerty",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: false),
                    senha_hash = table.Column<string>(type: "text", nullable: false),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "config_empresa",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "historico_cobranca",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "servicos",
                schema: "alerty");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "alerty");
        }
    }
}
