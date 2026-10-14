# Regras de negócio — Controle de Presença e Controle de Graduação

> Fase 3 do [ROADMAP.md](../ROADMAP.md). Definido em sessão de alinhamento em 15/set/2026, antes de
> qualquer linha de código — mesmo processo usado pro motor de alertas (WAHA).

## Controle de Presença

### Turma
Grade recorrente semanal, não aula avulsa. Cadastro único por horário fixo (ex.: "Jiu-jitsu Ter/Qui
19h"); o sistema resolve a ocorrência do dia (Turma + Data) sob demanda ao abrir a chamada — não há
job pré-gerando aulas futuras.

- `IdEmpresa`, `IdServico` (modalidade), `Nome` (opcional), `DiaSemana`, `HoraInicio`, `HoraFim`, `Ativo`.
- **Sem professor por enquanto.** Não existe papel de "funcionário" no sistema ainda (isso é Fase 5 —
  Isolamento de tenant). Quem lança a chamada é sempre o usuário logado (dono/admin).

### Matrícula em turma
Vínculo de referência, não exclusivo. Cliente tem uma (ou mais, uma por modalidade) turma principal,
mas a chamada de uma Turma sempre lista **todos os alunos ativos da modalidade** daquela turma — os
matriculados na turma aparecem primeiro/destacados. Isso permite reposição ocasional em outro horário
sem fricção, mas incentiva o aluno a frequentar seu horário de referência.

### Presença
Chamada completa: para cada aula (Turma + Data), lista os alunos elegíveis e marca presente/ausente
um a um — não é só "lançar quem veio". Isso é o que permite calcular frequência real (não só contagem
de presenças) pro requisito de graduação.

- Registro por cliente: `IdTurma`, `Data`, `Status` (Presente/Ausente), `LancadoPor` (id do usuário),
  `DataHoraLancamento`.
- Lançamento é **manual do professor/dono** por enquanto. Desenhado para trocar o "como" no futuro
  (QR code na academia, app do aluno) sem mexer no que já existe: muda a origem do lançamento, não o
  formato do registro de `Presenca`.

## Controle de Graduação

### Graduação (cadastro por modalidade, ordenado)
Lista ordenada de graduações por `IdServico` (ex.: Branca → Azul → Roxa → Marrom → Preta no
Jiu-jitsu). Cada nível da progressão tem seu próprio requisito — não existe um requisito único pra
modalidade inteira.

- `IdEmpresa`, `IdServico`, `Nome`, `Ordem`, `RequisitoDias` (tempo mínimo na graduação atual antes de
  poder subir), `RequisitoPresencas` (frequência mínima desde a última promoção).

### Estado atual do cliente
Por modalidade matriculada: graduação atual + data da última graduação. Ao matricular numa modalidade
nova, o cliente entra na primeira graduação da lista (menor `Ordem`), com data = data da matrícula.

### Elegibilidade ("apto a graduar") — calculada, não automática
```
apto = (hoje - data_ultima_graduacao >= RequisitoDias da próxima graduação)
       E (contagem de Presenca(Status=Presente) do cliente nessa modalidade desde
          data_ultima_graduacao >= RequisitoPresencas da próxima graduação)
```

O sistema **só sinaliza** no relatório "alunos aptos a se graduar" (separado por modalidade). A
promoção em si é sempre um ato manual do dono/professor — normalmente numa cerimônia — nunca
automática, porque frequência e tempo não substituem avaliação técnica.

Histórico de graduação é **imutável**, mesmo padrão do `historico_pagamento`: registra graduação
anterior, graduação nova, data e quem confirmou. Nunca é editado, só criado.

### Decisões de fluxo (definidas em sessão de 15/set/2026, após Presença já implementada)

- **Graduação inicial ao matricular**: campo opcional no fluxo de matrícula do cliente na modalidade.
  Se o dono não informar, o cliente entra na primeira graduação da lista (menor `Ordem`) daquela
  modalidade. Cobre o caso do aluno transferido de outra academia que já chega graduado.
- **Promoção nunca é bloqueada pelo sistema**: o endpoint de graduar sempre permite, mesmo que o
  cliente não tenha batido frequência/tempo mínimo. O cálculo de elegibilidade é só informativo — a
  avaliação técnica do professor sempre pode superar a regra.
- **Promoção sempre avança para a próxima graduação da ordem cadastrada.** Não existe endpoint pra
  "pular" ou escolher livremente uma graduação — simplifica o fluxo e mantém o histórico confiável.
  Correção manual de erro de lançamento, se precisar, é caso raro e fica fora do escopo por ora.
- **Modelo padrão sugerido**: só para Jiu-Jitsu adulto (Branca → Azul → Roxa → Marrom → Preta), porque
  é a modalidade da CT Fuzille (ambiente de validação) e a mais padronizada entre academias. O modelo
  preenche só nome e ordem — `RequisitoDias`/`RequisitoPresencas` ficam zerados, o dono preenche com o
  critério real da academia. Outras modalidades cadastram graduações 100% na mão. O botão "Usar modelo
  padrão" fica disponível sempre na tela de cadastro de graduações (não há detecção automática por
  nome de modalidade — o dono decide quando faz sentido usar).

## Ordem de implementação

Presença primeiro (backend completo: `Turma`, `Presenca`, services, endpoints, migration, testes) —
✓ feito, com frontend também. Graduação depende dele para calcular o requisito de frequência, entra
na sequência reaproveitando os dados de `Presenca` já existentes — ✓ feito também, backend e frontend.

## Status ao final da sessão de 15/set/2026

Os dois módulos (Presença e Graduação) estão **completos**: regras definidas, backend implementado e
testado (87 testes xUnit), frontend implementado e testado no navegador (Playwright), migrations
aplicadas no banco de dev.

**Frontend entregue:**
- Presença: `Turmas.tsx` (CRUD de turma) + `Chamada.tsx` (lançar presença por data)
- Graduação: `GraduacoesCadastro.tsx` (CRUD de faixas por modalidade, acessível por um botão em cada
  linha de `Modalidades.tsx`, com botão de modelo padrão) + `GraduacaoRelatorio.tsx` (progresso de
  todos os alunos, barra ordenada, ação de graduar direto) + dropdown de graduação ao lado de cada
  modalidade no `ModalEditar.tsx` de cliente

**Decisão pendente, resolvida:** a "graduação inicial ao matricular" não ficou embutida no
formulário de cadastro/edição de cliente (motivo técnico: o dropdown precisa do `id` do cliente já
existente). O dono confirmou que pode ficar como está — não é mais uma pendência.

**Bug encontrado e corrigido nessa sessão (não relacionado aos módulos acima):** `.relatorio-main` e
`.paineladm-main` tinham `padding-top` menor que a altura do header fixo (60px), cortando conteúdo em
telas de desktop. Corrigido pra 84px, padrão das demais páginas. Causa raiz: `App.css` define
`.pagina { padding-top: 60px }` mas esse arquivo **nunca é importado** — é CSS morto, cada página
compensa o header sozinha na própria classe `-main`.

**Estado do git ao fechar a sessão:**
- Repo `alerty-frontend`: só o fix do bug de padding foi commitado e pushado (`origin/main`). Todo o
  resto (Turmas, Chamada, Graduação — páginas, modais, tipos, mudanças em `App.tsx`/`Header.tsx`/
  `Modalidades.tsx`/`ModalCadastrar.tsx`/`ModalEditar.tsx`/`Modal.css`) está no working directory,
  **sem commit**, por pedido explícito do usuário.
- Repo `alerty-api-dotnet` (backend): **nada commitado** — todo o backend de Presença e Graduação
  (models, services, endpoints, migrations, testes) segue no working directory.

**Próxima sessão:** perguntar se já pode commitar/pushar o resto (backend + frontend das
funcionalidades novas) antes de continuar qualquer coisa nova, pra não acumular ainda mais working
tree sem versionar.
