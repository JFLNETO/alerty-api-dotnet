# CLAUDE.md — Alerty

> Este arquivo é lido automaticamente por qualquer instância do Claude Code que abrir este diretório.
> Ele funciona como a "memória de projeto": convenções e decisões de arquitetura já tomadas, para não
> precisar reexplicar tudo a cada sessão. Se alguma instrução aqui conflitar com o código real, o
> código vence — mas avise o William, pode ser um desvio não intencional.

## 1. O que é o Alerty

SaaS de gestão de academias de luta (CTs): cadastro de alunos, mensalidades com automação de Pix,
controle de presença (planejado) e notificações via WhatsApp. Nasceu como app Bubble.io + automações
n8n rodando para a CT Fuzille (a própria academia do dono) e está sendo migrado e produtizado como
backend C#/.NET real, com o Bubble/n8n sendo aposentado aos poucos.

**CT Fuzille é o ambiente de validação**: toda funcionalidade nova é testada lá antes de ir para os
outros tenants pagantes.

## 2. Estrutura de pastas (é um monorepo na prática)

```
alerty-api-dotnet/              <- raiz deste repo (backend)
├── Endpoints/                   <- Minimal API, um arquivo por recurso
├── Services/                    <- TODA a regra de negócio mora aqui
├── Models/                       <- POCOs simples, sem anotações de EF
├── DTOs/                         <- Criar*Request / Atualizar*Request, um por operação
├── Data/AppDbContext.cs          <- Fluent API: schema "alerty", colunas em snake_case
├── Exceptions/AppException.cs
├── Migrations/
├── Alerty.Tests/                 <- xUnit + EF Core InMemory
└── alerty-frontend/               <- repositório git PRÓPRIO, ignorado no .gitignore do backend
    ├── src/pages/                  <- uma página por rota
    ├── src/components/             <- principalmente Modais reutilizáveis
    ├── src/services/api.ts         <- ÚNICO lugar que fala com o backend
    ├── src/services/auth.ts        <- login/registro/logout, token em localStorage
    ├── src/types/                  <- interfaces + funções auxiliares de domínio
    └── src/styles/                 <- 1 CSS por página/componente, sem Tailwind
```

`alerty-frontend/` é outro repositório Git — só está fisicamente dentro da pasta do backend no seu
disco. Ao abrir o Claude Code na raiz, ele enxerga os dois ao mesmo tempo: trate os commits de cada
um separadamente e não misture as duas coisas num commit só sem necessidade.

## 3. Stack

- Backend: ASP.NET Core **Minimal API**, .NET 10 (versão travada no `global.json`)
- ORM: Entity Framework Core + Npgsql, banco **Neon Postgres** (schema `alerty`)
- Frontend: React + TypeScript + Vite — sem Redux/Zustand, estado local por página (`useState`/`useEffect`)
- Auth: JWT (access 8h / refresh 30d) + BCrypt; claims incluem `id_empresa` e `is_admin`
- Storage: AWS S3 via URL pré-assinada gerada pelo backend (`S3UploadService`)
- Deploy: Railway (Docker) para o backend, Vercel para o frontend
- Testes: xUnit + EF Core InMemory (`Alerty.Tests/DbContextFactory.Criar()`)

## 4. Regra de ouro: multitenancy

`id_empresa` **nunca** vem do body, da query string ou de qualquer input do cliente. Ele sempre é
extraído da claim do JWT:

```csharp
var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
```

Isso já foi uma vulnerabilidade real (IDOR) neste projeto e já foi corrigida — não reintroduzir.
Todo método de `Service` que busca ou altera dado de cliente deve filtrar por `idEmpresa` **e** pelo
`id` do registro juntos (ver `ClienteService.BuscarPorId`, `Atualizar`, `AlterarStatus` como
referência de padrão).

## 5. Padrão de Endpoint (Minimal API)

Um arquivo estático por recurso em `Endpoints/`, com um método de extensão `MapXEndpoints`:

```csharp
public static class ClienteEndpoints
{
    public static void MapClienteEndpoints(this WebApplication app)
    {
        app.MapGet("/clientes", (ClaimsPrincipal user, ..., ClienteService service) =>
        {
            var idEmpresa = int.Parse(user.FindFirst("id_empresa")!.Value);
            return Results.Ok(service.Listar(idEmpresa, ...));
        }).RequireAuthorization();
    }
}
```

Regras:
- Endpoint = tradução HTTP. Sem regra de negócio, sem `try/catch` manual ali dentro.
- Toda rota autenticada leva `.RequireAuthorization()` (ou a policy `"Admin"` quando for admin-only).
- Todo `MapXEndpoints()` novo precisa ser registrado em `Program.cs`.

## 6. Padrão de Service (onde a regra de negócio mora)

- Recebe `AppDbContext` via injeção de dependência no construtor — sem camada extra de "Repository".
- Valida regra de negócio lançando `AppException("mensagem para o usuário")`. Isso não é estilo, é
  o único jeito do middleware global devolver um 400 com mensagem legível em vez de um 500 genérico.
- Qualquer outra exceção (bug, falha de infra) sobe sem tratamento e vira 500 automaticamente — não
  silenciar com `try/catch` dentro do Service.

## 7. Modelos e mapeamento

- `Models/` são POCOs simples, sem atributos de EF.
- Todo mapeamento (nome de tabela, schema `alerty`, coluna snake_case) fica centralizado em
  `Data/AppDbContext.cs`, dentro de `OnModelCreating`.
- Ao adicionar uma coluna: editar o Model + `OnModelCreating` + gerar migration
  (`dotnet ef migrations add NomeDaMigration`).

## 8. DTOs

- Um DTO por operação: `Criar<Entidade>Request`, `Atualizar<Entidade>Request` — nunca reaproveitar
  o Model como corpo de request.
- Atualização parcial: no `Service.Atualizar`, só sobrescreve o campo se ele veio preenchido (ver
  `ClienteService.Atualizar` como referência desse padrão "PATCH-like" dentro de um PUT).

## 9. Tratamento de erro

- O `UseExceptionHandler` em `Program.cs` é a ÚNICA fonte de resposta de erro da API.
- `AppException` → 400 + `{ "erro": mensagem }`.
- Qualquer outra exceção → log + 500 genérico (`"Ocorreu um erro interno..."`) — nunca vaza stack
  trace pro cliente.

## 10. Testes

- xUnit, um arquivo `<Entidade>ServiceTests.cs` por Service.
- Banco: `DbContextFactory.Criar()` (EF Core InMemory, nome único por chamada — isolamento).
- Nome do teste: `Metodo_DeveComportamentoEsperado_QuandoCondicao`.
- Comentários `// Arrange` / `// Act` / `// Assert`, mesmo em testes curtos.
- Rodar `dotnet test` antes de considerar qualquer mudança em `Services/` concluída.

## 11. Frontend — padrões

- Toda chamada HTTP passa por `src/services/api.ts`
  (`apiGet` / `apiPost` / `apiPut` / `apiPatch` / `apiDelete`). Nunca `fetch` direto dentro de um
  componente ou página.
- 401 na `api.ts` já tenta renovar o token automaticamente via `/auth/refresh` antes de deslogar —
  não duplicar essa lógica em outro lugar.
- Sessão fica em `localStorage`: `accessToken`, `refreshToken`, `idEmpresa`, `isAdmin`, `nomeDono`.
- Uma página por rota em `src/pages/`; modais reutilizáveis em `src/components/`.
- Tipos + funções auxiliares de domínio (ex.: `getStatusCliente`, `formatarData`) ficam juntos em
  `src/types/<Entidade>.ts` — não espalhar lógica de "vencido/em dia" pela UI.
- CSS: um arquivo por página/componente em `src/styles/`. Sem Tailwind, sem CSS-in-JS.
- Ícones: `lucide-react`.

## 12. Glossário de domínio (não traduzir estes termos)

| Termo | Significado |
|---|---|
| `id_empresa` | tenant — a academia/CT dona do dado |
| `cliente` | aluno da academia |
| `servico` / `modalidade` | modalidade de luta (Jiu-jitsu, Muay Thai...), com valor e recorrência |
| `selos` | marcadores visuais do cliente (array) |
| `regra_alerta` | regra de quando disparar notificação (ex.: "1 dia antes do vencimento") |
| `waha_session` | sessão da ponte de WhatsApp (WAHA) usada para envio de mensagens |
| `historico_pagamento` | registro imutável de cada pagamento confirmado |

## 13. Segurança — não negociável

- `id_empresa` sempre do JWT, nunca de input do cliente (ver seção 4).
- Nunca commitar `appsettings.Development.json`, `appsettings.Local.json` ou credenciais AWS — já
  estão no `.gitignore`, manter assim.
- Upload de foto de cliente sempre via URL pré-assinada gerada pelo backend
  (`S3UploadService.GerarUrlUpload`) — nunca expor `Aws:AccessKeyId` / `Aws:SecretAccessKey` no
  frontend.
- Antes de trazer o segundo cliente pagante: trocar o JWT secret por um valor aleatório forte,
  adicionar rate limiting no `/auth/login` e configurar backup automático do Neon.

## 14. Pendências conhecidas (não resolver sem confirmar com o Neto)

- `.vscode/settings.json` ainda está versionado no repo do backend mesmo com `.vscode/` no
  `.gitignore` — foi commitado antes da regra existir. Precisa de `git rm --cached` explícito para sair.
- Hardening de segurança da seção 13 ainda não implementado.
- Reconhecimento facial (AWS Rekognition) está só avaliado, bloqueado por LGPD — não implementar sem
  fluxo de consentimento opt-in explícito.

## 15. Como trabalhar comigo (Claude Code) neste projeto

- Explique o "porquê" de cada mudança de arquitetura, não só o "o quê" — Neto está migrando de
  low-code (Bubble/n8n) para .NET e usa isso para aprender de verdade, não só pra entregar rápido.
- Ao sugerir algo nas camadas de Service/Endpoint, aponte o arquivo existente mais parecido (ex.:
  `ClienteService.cs`) como referência de padrão, em vez de inventar uma convenção nova do zero.
- Rode `dotnet build` + `dotnet test` (backend) ou `npm run build` (frontend) antes de dizer que uma
  tarefa está pronta.
- Se travar num erro de compilação, trate como diagnóstico e insista por alguns minutos antes de
  propor reverter tudo — é uma prática que o William está treinando de propósito.

## 16. Roadmap

Ver [ROADMAP.md](ROADMAP.md) para o plano de fases do produto (do primeiro cliente piloto ao SaaS
vendível a múltiplos clientes).
