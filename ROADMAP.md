# Do primeiro cliente ao SaaS vendível

> Origem: [conversa no claude.ai](https://claude.ai/chat/e8a5f4db-caef-4304-af49-15df0faac270).
> Documento vivo — atualizado conforme o trabalho avança. Última atualização: 15 set 2026.

Sete fases, em ordem de dependência: primeiro o que impede qualquer cliente de usar o produto com
segurança, depois o produto em si, depois o que falta para vender a mais de um.

## Visão geral

| Fase | Status | Esforço |
|---|---|---|
| 0 — Estancar o sangramento | ✓ concluída | — |
| 1 — O produto de verdade | ✓ concluída | — |
| 2 — Onboarding de empresa + frontend do motor de alertas | ✓ concluída | — |
| 3 — Novas funcionalidades de produto | em andamento | 2–4 sem |
| 4 — Base para não quebrar sem avisar | pendente | 1–2 sem |
| 5 — Isolamento de tenant estrutural | pendente | 1–2 sem |
| 6 — Monetização | pendente | 2–3 sem |

Restante estimado (fases 3–6), um dev full-time, sequencial: **≈ 6–11 semanas**.

Legenda de prioridade: **crítico** (quebra confiança ou dados) · **alto** (bloqueia uso sério) ·
**médio** (importa, não trava). Esforço: **S / M / L** (relativo).

---

## 0. Estancar o sangramento — ✓ concluída

Fechar os três buracos que já existem em produção hoje, antes de construir qualquer coisa nova em
cima deles.

**Por quê agora:** são correções pontuais e baratas — deixar para depois só aumenta o raio de dados
afetados (clientes reais, credenciais vivas).

| Tarefa | Prioridade | Esforço | Depende de |
|---|---|---|---|
| Corrigir `ClienteService.Atualizar`: `IdEmpresa` nunca vem do request, sempre do claim JWT | crítico | S | — |
| Tirar as chaves AWS do bundle do frontend; upload de foto passa pelo backend (URL pré-assinada ou proxy) | crítico | S | — |
| Middleware global de exceção + formato de erro único em todos os endpoints | alto | S | — |

## 1. O produto de verdade — ✓ concluída

Construir o motor de alertas — a razão do produto se chamar Alerty. Sem isso, tudo mais é um CRUD de
clientes.

**Decisão tomada:** envio via WAHA (gateway não-oficial, uma sessão de WhatsApp por empresa). Regras
de alerta configuráveis por empresa, limitadas pela quantidade do plano (básico=1, médio=3, plus=5).

| Entregue | Status | Esforço | Depende de |
|---|---|---|---|
| `RegraAlerta`: tipo (antes/no dia/após vencimento), dias e mensagem configuráveis por empresa, com CRUD via API | ✓ | M | — |
| `AlertaBackgroundService`: varredura diária, evita duplicidade via `HistoricoNotificacao`, envia relatório diário ao dono | ✓ | L | — |
| `WhatsAppService`: cliente HTTP do WAHA, sessão por empresa (`ConfigEmpresa.WahaSession`) | ✓ | M | — |

## 2. Onboarding de empresa + frontend do motor de alertas — ✓ concluída

Fechar o ciclo do que foi construído na Fase 1 — antes só dava pra configurar alerta e conectar o
WhatsApp via chamada HTTP direta (Postman/Swagger).

Escopo cresceu no meio do caminho: faltava uma tela de modalidades (nunca existiu CRUD, só leitura)
e, mais importante, o onboarding original só gerava a sessão do WAHA — não existia fluxo de cadastro
de empresa nenhum no frontend, só a tela de login. Foi preciso desenhar o fluxo completo (o que criar
automaticamente, seleção de plano, destino pós-cadastro) antes de construir a página.

| Entregue | Status | Esforço | Depende de |
|---|---|---|---|
| Frontend: página de Modalidades (CRUD completo de `Servico` — antes só existia leitura, sem tela nenhuma) | ✓ | M | — |
| Backend: endpoints proxy pro WAHA (iniciar sessão, status, QR code, pairing code) com sessão isolada por empresa (`ConfigEmpresa.WahaSession`) | ✓ | M | — |
| Frontend: página de conexão do WhatsApp — alterna QR code / código de pareamento, com polling de status | ✓ | M | — |
| Frontend: página de cadastro de alertas (criar/editar/ativar/desativar, respeitando o limite do plano) | ✓ | M | — |
| Fluxo completo de criação de empresa: página de registro (dados da academia + dono), auto-login, regra de alerta padrão criada na hora, sessão WAHA gerada automaticamente | ✓ | L | — |
| Frontend: tela de boas-vindas pós-cadastro com checklist guiado (modalidade → WhatsApp → revisar alerta) | ✓ | S | — |
| Migrations do EF Core adotadas em produção via baseline (schema existente preservado, sem perda de dado) | ✓ | M | — |
| Responsividade mobile em todo o frontend (menu do header, listas, tabelas, modais) + correções de UX (nome do cliente no relatório em vez do id, logo no login/registro) | ✓ | M | — |
| Teste ponta a ponta com clientes reais de uma empresa piloto | pendente | S | WAHA no ar (container + env vars no Railway) |

## 3. Novas funcionalidades de produto — em andamento

Expandir o Alerty além de mensalidade e vencimento: presença, planos por quantidade de aula e
progressão de graduação por modalidade.

São pedidos de produto reais, cada um passou por uma rodada de definição de regra de negócio antes de
virar código — o mesmo processo que fizemos pro motor de alertas (WAHA, regra por plano, etc). Regras
documentadas em [docs/regras-presenca-graduacao.md](docs/regras-presenca-graduacao.md). Incremental:
pode rodar em paralelo com as fases de infraestrutura abaixo, uma não bloqueia a outra.

| Entregue | Status | Esforço | Depende de |
|---|---|---|---|
| Controle de presença: `Turma` (grade recorrente por modalidade) + `Presenca` (chamada completa por turma/data), backend e frontend (`Turmas`, `Chamada`) | ✓ | L | — |
| Controle de graduação: cadastro de faixas por modalidade (com modelo padrão sugerido pro Jiu-Jitsu adulto), estado atual do cliente por modalidade, promoção manual nunca bloqueada por requisito, backend e frontend (`GraduacoesCadastro`, dropdown no cliente) | ✓ | L | Controle de presença (frequência entra no cálculo) |
| Frontend: relatório de progressão de graduação — todos os alunos, barra de progresso, ordenado por progresso, ação de graduar direto quando apto | ✓ | S | Controle de graduação |
| Planos com quantidade de aulas disponíveis por semana (limite semanal + controle de consumo) | pendente | M | — |
| Aniversário do cliente: campo `DataNascimento` em `Cliente` + alerta automático de mensagem de parabéns no dia, reaproveitando `WhatsAppService`/motor de alertas já existente | pendente | S | — |

## 4. Base para não quebrar sem avisar — 1–2 semanas

Dar ao time meios de saber quando algo falhar — o job de alertas vai falhar em algum caso de vez em
quando.

**Por quê agora:** sem log estruturado e health check, uma falha silenciosa no job passa despercebida
até um cliente reclamar do vencimento que não avisou.

| Tarefa | Prioridade | Esforço | Depende de |
|---|---|---|---|
| Logging estruturado (Serilog) + endpoint de health check | alto | S | — |
| CI mínimo: rodar a suíte xUnit em cada PR | alto | S | — |
| Testes para `AuthService` (login, refresh, JWT) | alto | M | — |
| Rate limiting e fluxo de recuperação de senha no login | médio | M | — |

## 5. Isolamento de tenant estrutural — 1–2 semanas

Trocar o filtro `.Where(IdEmpresa == ...)` copiado à mão em cada endpoint por uma garantia central —
e dar à empresa-cliente gestão da própria conta.

**Por quê agora, e não antes:** com um único cliente piloto o risco de vazamento entre empresas é
teórico; assim que existir um segundo cliente pagante, vira um risco real de dados de uma academia
aparecerem para outra.

| Tarefa | Prioridade | Esforço | Depende de |
|---|---|---|---|
| Global query filter por `IdEmpresa` no `AppDbContext` + `ITenantProvider` resolvido a partir do JWT | crítico | M | — |
| Papéis dentro da empresa (dono vs. funcionário) e convite de novos usuários | alto | M | — |
| Endpoints de autoatendimento: editar perfil da empresa, ver uso vs. limite | médio | S | — |

## 6. Monetização — 2–3 semanas

Cobrar as empresas-cliente pelo uso e proteger a API para receber integrações externas sem quebrar a
cada mudança.

**Por quê por último:** só compensa depois de validar o produto com clientes reais nas fases
anteriores — cobrar cedo demais por algo que ainda não avisa vencimento é vender promessa.

| Tarefa | Prioridade | Esforço | Depende de |
|---|---|---|---|
| Modelo de plano/assinatura + gateway de cobrança (Stripe ou Pix/gateway nacional) | alto | L | Fase 5 |
| Aplicar `LimiteNotificacoes` e cotas por plano de fato (hoje é campo morto) | médio | S | modelo de plano |
| Dividir funcionalidades do sistema de acordo com o plano contratado — feature gating de verdade (ex: presença/graduação só em planos maiores), não só cota de notificação | médio | M | modelo de plano |
| Versionar a API (`/v1`) antes de abrir para integrações externas | médio | M | — |
| Swagger acessível em produção (público ou autenticado) | médio | S | — |

---

## Onde parar, dependendo do objetivo

**Só validar com 1 cliente piloto**
Fases 0–2 já estão concluídas — falta só subir o container do WAHA em produção (Railway) e fazer o
teste ponta a ponta. Idealmente a Fase 4 também, antes de confiar o piloto de olhos fechados.

**Vender como SaaS a vários clientes**
Fases 0–6 completas. Fase 5 principalmente — é o que impede dados de uma empresa vazarem para outra
quando não há mais só um cliente confiável observando.

A Fase 3 (novas funcionalidades) é independente das demais — pode ser feita em paralelo, intercalada,
ou adiada sem travar o resto do roadmap. Cada linha das tabelas acima pode virar uma issue/ticket
separado quando quiserem começar a execução.
