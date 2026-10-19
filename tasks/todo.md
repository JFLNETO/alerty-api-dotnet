# Todo: Aviso ao dono quando o telefone do aluno falha no WhatsApp

Plano e decisões em [plan.md](plan.md).

## Task 1: Deep link `/?cliente={id}` abre o modal do cliente

**Description:** Na página `Clientes`, ao montar, ler `?cliente=` com `useSearchParams`. Se houver um
id numérico, chamar `apiGet<Cliente>('/clientes/{id}')`, abrir o `ModalEditar` com o retorno e
remover o parâmetro da URL (`setSearchParams` com `replace: true`), para que um F5 não reabra o
modal. Se a chamada falhar (404 ou cliente de outra empresa), não fazer nada: a lista abre normal.

**Acceptance criteria:**
- [x] `/?cliente=<id existente>` abre o `ModalEditar` daquele aluno, mesmo se ele estiver inativo
- [x] `/?cliente=999999` e `/?cliente=abc` abrem a lista sem erro na tela
- [x] Depois de abrir, a URL volta a ser `/`, e fechar o modal não reabre nada

**Verification:**
- [x] Build succeeds: `npm run build` (em `alerty-frontend/`)
- [ ] Manual check: `npm run dev`, abrir os três casos acima logado na CT Fuzille

**Dependencies:** None

**Files likely touched:**
- `alerty-frontend/src/pages/Clientes.tsx`

**Estimated scope:** XS

## Task 2: O login preserva o destino

**Description:** `RotaProtegida` (em `App.tsx`) passa `state={{ from: location }}` no `<Navigate
to="/login">`. O `Login`, depois de autenticar, navega para `from.pathname + from.search` quando
existir, e para `/` quando não existir. Só aceitar caminhos internos (que começam com `/`), para não
virar open redirect.

**Acceptance criteria:**
- [ ] Deslogado, abrir `/?cliente=<id>` → tela de login → após login cai no modal daquele aluno
- [x] Login normal (sem destino salvo) continua indo para `/`
- [x] A rota de admin (`/paineladm`) continua com o mesmo comportamento de bloqueio de hoje

**Verification:**
- [x] Build succeeds: `npm run build`
- [ ] Manual check: aba anônima, colar o link, logar

**Dependencies:** Task 1

**Files likely touched:**
- `alerty-frontend/src/App.tsx`
- `alerty-frontend/src/pages/Login.tsx`

**Estimated scope:** S

## Checkpoint: Frontend
- [x] `npm run build` sem erros
- [ ] Fluxo logado e deslogado testado manualmente
- [ ] Commit no repo `alerty-frontend` (separado do backend) e deploy na Vercel
- [ ] Revisar com o Neto antes de seguir para o backend

## Task 3: `WhatsAppService` classifica o motivo da falha

**Description:** Criar o enum `MotivoFalhaEnvio { Nenhum, TelefoneInvalido, NumeroSemWhatsApp, Outro }`
e o `record ResultadoEnvio(bool Sucesso, string? Erro, MotivoFalhaEnvio Motivo)`. O `EnviarAsync`
passa a devolver o `ResultadoEnvio`. Antes de chamar o WAHA, se `RemoverDdi(telefone)` não tiver 10
ou 11 dígitos, devolver `TelefoneInvalido`. Quando `check-exists` disser `NumberExists = false`,
devolver `NumeroSemWhatsApp`. Quando o 500 do WAHA tiver "no LID found" no corpo, também
`NumeroSemWhatsApp`. O resto (WAHA não configurado, sem sessão, outros status, exceção) devolve
`Outro`. Ajustar os 4 pontos que chamam em `AlertaJobService` para usar o record, sem mudar
comportamento ainda.

**Acceptance criteria:**
- [x] Telefone vazio, com 8 dígitos ou com 14 dígitos → `TelefoneInvalido`, **sem** nenhuma chamada HTTP ao WAHA
- [x] `check-exists` com `numberExists: false` → `NumeroSemWhatsApp`; 500 com "no LID found" → `NumeroSemWhatsApp`
- [x] Sucesso → `Motivo = Nenhum`; outras falhas → `Outro`; mensagens de `Erro` iguais às de hoje

**Verification:**
- [x] Tests pass: `dotnet test --filter WhatsAppServiceTests` (novos casos usando o `HandlerFalso` que já existe)
- [x] Build succeeds: `dotnet build`

**Dependencies:** None (pode ser feita em paralelo com as Tasks 1–2)

**Files likely touched:**
- `Services/WhatsAppService.cs`
- `Models/MotivoFalhaEnvio.cs` (novo, seguindo `Models/TipoAlerta.cs`)
- `Services/AlertaJobService.cs` (só adaptar os 4 pontos que chamam)
- `Alerty.Tests/WhatsAppServiceTests.cs`

**Estimated scope:** M

## Task 4: `AlertaJobService` avisa o dono sobre contatos inválidos, com link

**Description:** Em `ProcessarFilaEmpresaAsync`, juntar os envios com `Motivo` igual a
`TelefoneInvalido` ou `NumeroSemWhatsApp`, sem repetir aluno (agrupar por `Cliente.Id`). Depois do
relatório do dono, se a lista não estiver vazia e `WhatsappDono` estiver preenchido, esperar o
intervalo aleatório e enviar pela `empresa.WahaSession` o texto gerado por
`MontarAvisoContatosInvalidos(...)`. Essa função é `internal static` e pura: recebe a lista de alunos
com problema e a `baseUrl`, e devolve o texto com uma linha por aluno (nome, motivo legível e
`{baseUrl}/?cliente={id}`). Ler `Frontend:BaseUrl` do `IConfiguration`. Se estiver vazia, mandar sem
link. Adicionar a chave em `appsettings.json` (valor vazio) e `InternalsVisibleTo` para o projeto
de testes, se ainda não existir. O aviso ao suporte (`NotificarSuporteAsync`) continua igual.

**Acceptance criteria:**
- [x] O aviso só lista alunos da própria empresa, e só falhas `TelefoneInvalido` / `NumeroSemWhatsApp` (erros de infra não entram)
- [x] Cada aluno aparece uma vez, com o link `https://www.checkygo.com.br/?cliente={id}` (ou sem link, se a config estiver vazia)
- [x] Nenhuma mensagem extra quando não houver falha desse tipo; o suporte continua recebendo o resumo de sempre

**Verification:**
- [x] Tests pass: `dotnet test` (novo `AlertaJobServiceTests.cs` cobrindo `MontarAvisoContatosInvalidos`: com link, sem link, aluno repetido)
- [x] Build succeeds: `dotnet build`
- [ ] Manual check: na CT Fuzille, cadastrar um aluno de teste com telefone `11111111` e vencimento que dispare uma regra; rodar a varredura pelo painel admin; o dono recebe o aviso e o link abre o modal

**Dependencies:** Task 3; Tasks 1–2 em produção antes do deploy

**Files likely touched:**
- `Services/AlertaJobService.cs`
- `appsettings.json`
- `Alerty.Api.csproj` (`InternalsVisibleTo`, se ainda não existir)
- `Alerty.Tests/AlertaJobServiceTests.cs` (novo)

**Estimated scope:** M

## Checkpoint: Completo
- [x] `dotnet build` + `dotnet test` verdes
- [ ] `Frontend__BaseUrl=https://www.checkygo.com.br` configurada no Railway antes do deploy
- [ ] Validado ponta a ponta na CT Fuzille
- [x] Respostas das perguntas em aberto do `plan.md` incorporadas (principalmente a repetição diária)
- [ ] SQL da migration `AdicionaTelefoneAvisoInvalidoCliente` aplicado no Neon (`neondb`) antes do push
