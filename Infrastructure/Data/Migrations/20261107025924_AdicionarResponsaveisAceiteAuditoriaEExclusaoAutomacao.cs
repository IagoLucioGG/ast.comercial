using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AST.Comercial.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarResponsaveisAceiteAuditoriaEExclusaoAutomacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EmpresaId",
                table: "RegistroAlteracao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UsuarioId",
                table: "RegistroAlteracao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioNome",
                table: "RegistroAlteracao",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRecusa",
                table: "Proposta",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondidaEm",
                table: "Proposta",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RespondidaIp",
                table: "Proposta",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RespondidaPorEmail",
                table: "Proposta",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RespondidaPorNome",
                table: "Proposta",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusResposta",
                table: "Proposta",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'Aguardando'");

            migrationBuilder.AddColumn<long>(
                name: "ResponsavelId",
                table: "Negocio",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CriadoPorUsuarioId",
                table: "Automacao",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Excluida",
                table: "Automacao",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ResponsavelId",
                table: "Atividade",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAlteracao_EmpresaId_DataHora",
                table: "RegistroAlteracao",
                columns: new[] { "EmpresaId", "DataHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Negocio_ResponsavelId",
                table: "Negocio",
                column: "ResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_Automacao_CriadoPorUsuarioId",
                table: "Automacao",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Atividade_ResponsavelId",
                table: "Atividade",
                column: "ResponsavelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Atividade_Usuario_ResponsavelId",
                table: "Atividade",
                column: "ResponsavelId",
                principalTable: "Usuario",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Automacao_Usuario_CriadoPorUsuarioId",
                table: "Automacao",
                column: "CriadoPorUsuarioId",
                principalTable: "Usuario",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Negocio_Usuario_ResponsavelId",
                table: "Negocio",
                column: "ResponsavelId",
                principalTable: "Usuario",
                principalColumn: "Id");

            // ------------------------------------------------------------------
            // Migração de dados (SQL). Idempotente e conservadora.
            // ------------------------------------------------------------------

            // 1) Automação: o marcador antigo ChaveExterna='__excluida__' vira a coluna Excluida.
            migrationBuilder.Sql(@"
                UPDATE ""Automacao"" SET ""Excluida"" = TRUE, ""ChaveExterna"" = NULL
                WHERE ""ChaveExterna"" = '__excluida__';");

            // 2) Proposta: converte a linha '[Resposta pública] ...' (gravada em Observacoes) nas colunas novas
            //    e remove a linha das Observacoes. Formato antigo:
            //    [Resposta pública] Aceita|Recusada em yyyy-MM-dd HH:mm:ss UTC; IP: x[; Nome: n][; E-mail: e][; Motivo: m]
            migrationBuilder.Sql(@"
                UPDATE ""Proposta"" p SET
                    ""StatusResposta"" = CASE WHEN x.m[1] = 'Aceita' THEN 'Aceita' ELSE 'Recusada' END,
                    ""RespondidaEm"" = (x.m[2] || '+00')::timestamptz,
                    ""RespondidaIp"" = LEFT(NULLIF(NULLIF(btrim(x.m[3]), ''), 'desconhecido'), 64),
                    ""RespondidaPorNome"" = LEFT(NULLIF(btrim(x.m[4]), ''), 200),
                    ""RespondidaPorEmail"" = LEFT(NULLIF(btrim(x.m[5]), ''), 254),
                    ""MotivoRecusa"" = LEFT(NULLIF(btrim(x.m[6]), ''), 1000),
                    ""Observacoes"" = NULLIF(btrim(regexp_replace(p.""Observacoes"", E'\\n?\\[Resposta pública\\][^\\n]*', '', 'g')), '')
                FROM (
                    SELECT ""Id"", regexp_match(""Observacoes"",
                        E'\\[Resposta pública\\] (Aceita|Recusada) em (\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}) UTC; IP: ([^;\\n]*)(?:; Nome: ([^;\\n]*))?(?:; E-mail: ([^;\\n]*))?(?:; Motivo: ([^\\n]*))?') AS m
                    FROM ""Proposta""
                    WHERE ""Observacoes"" LIKE '%[Resposta pública]%'
                ) x
                WHERE p.""Id"" = x.""Id"" AND x.m IS NOT NULL AND p.""StatusResposta"" = 'Aguardando';");

            // 3) RegistroAlteracao: nome do usuário e Id (quando o nome identifica um único usuário da empresa).
            migrationBuilder.Sql(@"UPDATE ""RegistroAlteracao"" SET ""UsuarioNome"" = ""Usuario"" WHERE ""UsuarioNome"" IS NULL AND ""Usuario"" NOT IN ('Sistema', 'Automação');");

            // 4) RegistroAlteracao.EmpresaId. Só preenche quando é inequívoco:
            //    a) existe uma única empresa no banco -> todas as linhas pertencem a ela;
            //    b) senão, herda a empresa do próprio registro auditado (mesma entidade e Id).
            //       Para 'Negocio' só quando Titulo não é nulo (a raiz auditada era o próprio negócio; sem título pode ser
            //       uma proposta avulsa cujo Id coincide com o de um negócio).
            //    Linhas que continuarem sem empresa ficam com EmpresaId NULL e NÃO aparecem na API.
            migrationBuilder.Sql(@"
                UPDATE ""RegistroAlteracao"" SET ""EmpresaId"" = (SELECT ""Id"" FROM ""Empresa"" LIMIT 1)
                WHERE ""EmpresaId"" IS NULL AND (SELECT COUNT(*) FROM ""Empresa"") = 1;");

            foreach (var (alvo, tabela) in new[]
            {
                ("Cliente", "Cliente"), ("PessoaContato", "PessoaContato"), ("Funil", "Funil"), ("EtapaFunil", "EtapaFunil"),
                ("Atividade", "Atividade"), ("Produto", "Produto"), ("Usuario", "Usuario"), ("Equipe", "Equipe"),
                ("Departamento", "Departamento"), ("Cargo", "Cargo"),
            })
            {
                migrationBuilder.Sql($@"
                    UPDATE ""RegistroAlteracao"" r SET ""EmpresaId"" = t.""EmpresaId""
                    FROM ""{tabela}"" t
                    WHERE r.""EmpresaId"" IS NULL AND r.""EntidadeAlvo"" = '{alvo}' AND t.""Id"" = r.""RegistroId"";");
            }

            migrationBuilder.Sql(@"
                UPDATE ""RegistroAlteracao"" r SET ""EmpresaId"" = t.""EmpresaId""
                FROM ""Negocio"" t
                WHERE r.""EmpresaId"" IS NULL AND r.""EntidadeAlvo"" = 'Negocio' AND r.""Titulo"" IS NOT NULL AND t.""Id"" = r.""RegistroId"";");

            migrationBuilder.Sql(@"
                UPDATE ""RegistroAlteracao"" SET ""EmpresaId"" = ""RegistroId""
                WHERE ""EmpresaId"" IS NULL AND ""EntidadeAlvo"" = 'Empresa' AND EXISTS (SELECT 1 FROM ""Empresa"" e WHERE e.""Id"" = ""RegistroAlteracao"".""RegistroId"");");

            // 5) Higiene: o auditor antigo gravava valores originais de qualquer coluna modificada, inclusive
            //    Usuario.SenhaHash/TokenIntegracao. Remove esses segredos dos snapshots já gravados.
            migrationBuilder.Sql(@"
                UPDATE ""RegistroAlteracao"" SET
                    ""DadosDepois"" = CASE WHEN ""DadosDepois"" IS NULL THEN NULL ELSE ""DadosDepois"" - 'SenhaHash' - 'TokenIntegracao' - 'Tokens' END,
                    ""DadosAntes"" = CASE WHEN ""DadosAntes"" IS NULL THEN NULL ELSE ""DadosAntes"" - 'SenhaHash' - 'TokenIntegracao' - 'Tokens' END
                WHERE ""DadosDepois""::text ~ 'SenhaHash|RefreshToken|TokenIntegracao' OR ""DadosAntes""::text ~ 'SenhaHash|RefreshToken|TokenIntegracao';");

            migrationBuilder.Sql(@"
                UPDATE ""RegistroAlteracao"" r SET ""UsuarioId"" = u.""Id""
                FROM ""Usuario"" u
                WHERE r.""UsuarioId"" IS NULL AND r.""EmpresaId"" = u.""EmpresaId"" AND r.""UsuarioNome"" = u.""Nome""
                  AND (SELECT COUNT(*) FROM ""Usuario"" u2 WHERE u2.""EmpresaId"" = u.""EmpresaId"" AND u2.""Nome"" = u.""Nome"") = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Atividade_Usuario_ResponsavelId",
                table: "Atividade");

            migrationBuilder.DropForeignKey(
                name: "FK_Automacao_Usuario_CriadoPorUsuarioId",
                table: "Automacao");

            migrationBuilder.DropForeignKey(
                name: "FK_Negocio_Usuario_ResponsavelId",
                table: "Negocio");

            migrationBuilder.DropIndex(
                name: "IX_RegistroAlteracao_EmpresaId_DataHora",
                table: "RegistroAlteracao");

            migrationBuilder.DropIndex(
                name: "IX_Negocio_ResponsavelId",
                table: "Negocio");

            migrationBuilder.DropIndex(
                name: "IX_Automacao_CriadoPorUsuarioId",
                table: "Automacao");

            migrationBuilder.DropIndex(
                name: "IX_Atividade_ResponsavelId",
                table: "Atividade");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "RegistroAlteracao");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "RegistroAlteracao");

            migrationBuilder.DropColumn(
                name: "UsuarioNome",
                table: "RegistroAlteracao");

            migrationBuilder.DropColumn(
                name: "MotivoRecusa",
                table: "Proposta");

            migrationBuilder.DropColumn(
                name: "RespondidaEm",
                table: "Proposta");

            migrationBuilder.DropColumn(
                name: "RespondidaIp",
                table: "Proposta");

            migrationBuilder.DropColumn(
                name: "RespondidaPorEmail",
                table: "Proposta");

            migrationBuilder.DropColumn(
                name: "RespondidaPorNome",
                table: "Proposta");

            migrationBuilder.DropColumn(
                name: "StatusResposta",
                table: "Proposta");

            migrationBuilder.DropColumn(
                name: "ResponsavelId",
                table: "Negocio");

            migrationBuilder.DropColumn(
                name: "CriadoPorUsuarioId",
                table: "Automacao");

            migrationBuilder.DropColumn(
                name: "Excluida",
                table: "Automacao");

            migrationBuilder.DropColumn(
                name: "ResponsavelId",
                table: "Atividade");
        }
    }
}
