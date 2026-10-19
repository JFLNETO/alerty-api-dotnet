# Implementation Plan: Aviso ao dono quando o telefone do aluno falha no WhatsApp

## Overview

Hoje, quando o job diário de alertas não consegue mandar mensagem para um aluno, só o suporte fica
sabendo (`AlertaJobService.NotificarSuporteAsync`, que manda para a sessão `empresa_7`). O dono da
academia não sabe de nada, e o aluno continua sem receber o aviso de vencimento.

A ideia é: quando a falha for **culpa do cadastro do aluno** (telefone inválido, ou número que não
tem WhatsApp), o dono também recebe uma mensagem pela sessão WAHA da própria empresa. Essa mensagem
lista os alunos com problema, e cada um vem com um link que abre direto o modal de edição daquele
aluno no CheckyGo, para o dono corrigir o telefone.

Falhas de infraestrutura (WAHA fora do ar, sessão desconectada, 500 genérico) continuam indo **só**
para o suporte. O dono não tem o que fazer com elas.

## Architecture Decisions

- **Classificar a falha com um enum, sem comparar texto.** Hoje `WhatsAppService.EnviarAsync`
  devolve `(bool Sucesso, string? Erro)`, e o erro é texto livre. Para decidir quem recebe o aviso,
  daria para comparar com `"Número não está registrado no WhatsApp."`, mas isso quebra no dia em que
  alguém mudar a frase. O plano é devolver um `record ResultadoEnvio(bool Sucesso, string? Erro,
  MotivoFalhaEnvio Motivo)`, com `MotivoFalhaEnvio { Nenhum, TelefoneInvalido, NumeroSemWhatsApp,
  Outro }`. O *porquê*: quem chama passa a tomar decisão por um valor tipado, que o compilador
  confere. Referência de padrão: `TipoAlerta` / `StatusPresenca` em `Models/`.
- **Validar o tamanho do telefone antes de chamar o WAHA.** Depois do `TelefoneUtils.RemoverDdi`, um
  telefone válido tem 10 ou 11 dígitos (DDD + número). Se não tiver, o envio é cancelado com
  `TelefoneInvalido`, sem gastar uma chamada ao WAHA. Isso também cobre o aluno sem telefone, que hoje
  vai para o WAHA como `""` e falha com um erro confuso.
- **"No LID found" no corpo do 500 do WAHA também conta como `NumeroSemWhatsApp`.** O `check-exists`
  pode falhar e o envio seguir mesmo assim (comportamento atual, mantido). Nesse caso, o erro que
  aparece para número inexistente é o 500 com "no LID found".
- **Um aviso por empresa, enviado depois do relatório do dono, pela sessão da própria empresa.** Ele
  vai na mesma fila sequencial (`ProcessarFilaEmpresaAsync`), com o mesmo intervalo aleatório de
  30–60s. Fica uma mensagem só, e não uma por aluno, para não aumentar o risco de bloqueio do número.
  Isolamento de tenant: o aviso de cada empresa só contém alunos dela, porque é montado dentro da fila
  daquele grupo. Isso é diferente do aviso ao suporte, que mistura tenants e por isso tem a trava de
  sessão única.
- **O formato do link é `{Frontend:BaseUrl}/?cliente={id}`.** O `Frontend:BaseUrl` é uma config nova
  (`https://www.checkygo.com.br` em produção, variável `Frontend__BaseUrl` no Railway). Se não
  estiver configurada, o aviso sai sem link (só os nomes), em vez de não sair. O id no link não é
  problema de segurança: o `GET /clientes/{id}` já filtra pelo `id_empresa` do JWT, então quem abrir o
  link logado em outra empresa recebe 404.
- **O frontend abre o modal pela query string e pede o cliente por id.** A página `Clientes` carrega
  com o filtro "ativos". Por isso, procurar o cliente na lista já carregada falharia quando ele não
  estivesse nela. O plano é chamar `apiGet('/clientes/{id}')`, abrir o `ModalEditar` e limpar o
  `?cliente=` da URL.
- **O login preserva o destino.** Hoje `RotaProtegida` manda para `/login` e o `Login` sempre volta
  para `/`, o que perde o `?cliente=`. O dono que clicar no link pelo celular, sem sessão, perderia o
  link. A solução padrão do react-router é passar `state={{ from: location }}` no `<Navigate>` e
  voltar para ele depois do login.
- **A geração do texto do aviso fica numa função pura e estática.** A `ProcessarFilaEmpresaAsync`
  tem `Task.Delay` de 30–60s e não dá para testar. A montagem da mensagem
  (`MontarAvisoContatosInvalidos`) vira um método `internal static`, testável no xUnit sem rede e
  sem espera.

## Task List

Ver [todo.md](todo.md) para os critérios de aceite de cada tarefa.

### Fase 1: O link funciona (frontend, repo `alerty-frontend`)
- [ ] Task 1: Deep link `/?cliente={id}` abre o modal do cliente
- [ ] Task 2: O login preserva o destino (`?cliente=` sobrevive ao redirecionamento)

### Checkpoint: Frontend
- [ ] `npm run build` limpo; link testado logado e deslogado; deploy na Vercel **antes** do backend

### Fase 2: O aviso sai (backend, este repo)
- [ ] Task 3: `WhatsAppService` classifica o motivo da falha (`MotivoFalhaEnvio`)
- [ ] Task 4: `AlertaJobService` avisa o dono sobre contatos inválidos, com link

### Checkpoint: Completo
- [ ] `dotnet build` + `dotnet test` verdes
- [ ] `Frontend__BaseUrl` configurada no Railway
- [ ] Validado na CT Fuzille com um aluno de teste com telefone inválido

A ordem é frontend primeiro: se o backend subir antes, o dono recebe um link que só abre a lista e
não faz nada. Sem migration nesta feature, porque nenhum schema muda.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| O dono recebe o mesmo aviso todo dia até corrigir o telefone (o job retenta, porque o histórico só bloqueia com `Sucesso = true`) | Med | Pode ser até desejado, como lembrete. Ver pergunta em aberto. |
| O próprio `WhatsappDono` está errado, e o aviso também falha | Low | Já é logado como warning. A falha do aviso não entra na lista de falhas dos alunos. |
| Mudar a assinatura de `EnviarAsync` quebra os 4 pontos que chamam | Low | Todos estão em `AlertaJobService`. O compilador aponta cada um. |
| A regra de 10/11 dígitos rejeita um número estrangeiro válido | Low | Hoje toda a base é BR (DDI 55 fixo em `NormalizarParaWhatsApp`). Registrar a limitação no comentário. |
| Um mesmo aluno aparece repetido no aviso porque duas regras disparam no mesmo dia | Low | Agrupar por `Cliente.Id` na montagem do aviso. |

## Decisões do Neto (19 out 2026)

- **Não repetir o aviso.** Nova coluna `clientes.telefone_aviso_invalido` (migration
  `AdicionaTelefoneAvisoInvalidoCliente`), com o telefone sobre o qual o dono já foi avisado. O aviso
  só sai quando o telefone atual é diferente desse valor. Se o dono corrigir para outro número que
  também falhe, recebe um aviso novo. A coluna só é gravada se o aviso chegou, e é limpa quando um
  envio dá certo.
- **"Enviar teste" do painel admin não avisa o dono.**
- **`empresa_7` continua recebendo todas as falhas**, sem mudança.
