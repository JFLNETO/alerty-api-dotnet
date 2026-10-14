public class ClienteGraduacaoServiceTests
{
    private static (AppDbContext db, ClienteGraduacaoService clienteGraduacaoService, ClienteService clienteService, GraduacaoService graduacaoService, Servico servico)
        CriarCenario(int idEmpresa = 13)
    {
        var db = DbContextFactory.Criar();
        var clienteGraduacaoService = new ClienteGraduacaoService(db);
        var clienteService = new ClienteService(db, clienteGraduacaoService);
        var graduacaoService = new GraduacaoService(db);
        var servico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, idEmpresa);

        return (db, clienteGraduacaoService, clienteService, graduacaoService, servico);
    }

    private static Cliente CriarCliente(ClienteService clienteService, int idServico, int idEmpresa = 13)
    {
        return clienteService.Criar(new CriarClienteRequest
        {
            Nome = "Aluno Teste",
            Telefone = "79999990000",
            DataVencimento = new DateOnly(2026, 12, 31),
            IdServicos = new[] { idServico }
        }, idEmpresa);
    }

    [Fact]
    public void SincronizarMatriculas_DeveCriarEstadoInicial_QuandoModalidadeTemGraduacoes()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Azul", Ordem = 2 }, 13);

        // Act
        var cliente = CriarCliente(clienteService, servico.Id);
        var estado = clienteGraduacaoService.ListarPorCliente(cliente.Id, 13);

        // Assert
        Assert.Single(estado);
        Assert.Equal("Branca", estado[0].NomeGraduacaoAtual);
        Assert.Equal("Azul", estado[0].NomeProximaGraduacao);
    }

    [Fact]
    public void SincronizarMatriculas_NaoDeveCriarEstado_QuandoModalidadeSemGraduacoes()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, _, servico) = CriarCenario();

        // Act
        var cliente = CriarCliente(clienteService, servico.Id);
        var estado = clienteGraduacaoService.ListarPorCliente(cliente.Id, 13);

        // Assert
        Assert.Empty(estado);
    }

    [Fact]
    public void SincronizarMatriculas_DeveCriarEstado_QuandoGraduacoesCadastradasDepoisDaMatricula()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        var cliente = CriarCliente(clienteService, servico.Id);
        Assert.Empty(clienteGraduacaoService.ListarPorCliente(cliente.Id, 13));

        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);

        // Act
        clienteService.Atualizar(cliente.Id, new AtualizarClienteRequest
        {
            IdServicos = new[] { servico.Id }
        }, 13);

        // Assert
        var estado = clienteGraduacaoService.ListarPorCliente(cliente.Id, 13);
        Assert.Single(estado);
        Assert.Equal("Branca", estado[0].NomeGraduacaoAtual);
    }

    [Fact]
    public void DefinirGraduacaoAtual_DeveSobrescreverEstado_QuandoAlunoJaChegaGraduado()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);
        var roxa = graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Roxa", Ordem = 2 }, 13);
        var cliente = CriarCliente(clienteService, servico.Id);

        // Act
        clienteGraduacaoService.DefinirGraduacaoAtual(cliente.Id, servico.Id, new DefinirGraduacaoAtualRequest
        {
            IdGraduacao = roxa.Id
        }, 13);

        // Assert
        var estado = clienteGraduacaoService.ListarPorCliente(cliente.Id, 13);
        Assert.Equal("Roxa", estado[0].NomeGraduacaoAtual);
    }

    [Fact]
    public void DefinirGraduacaoAtual_DeveLancarExcecao_QuandoClienteNaoMatriculadoNaModalidade()
    {
        // Arrange
        var (db, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        var outroServico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Muay Thai" }, 13);
        var graduacao = graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = outroServico.Id, Nome = "Nível 1", Ordem = 1 }, 13);
        var cliente = CriarCliente(clienteService, servico.Id);

        // Act
        var ex = Assert.Throws<AppException>(() => clienteGraduacaoService.DefinirGraduacaoAtual(
            cliente.Id, outroServico.Id, new DefinirGraduacaoAtualRequest { IdGraduacao = graduacao.Id }, 13));

        // Assert
        Assert.Equal("Cliente não está matriculado nessa modalidade.", ex.Message);
    }

    [Fact]
    public void Graduar_DevePromoverParaProximaGraduacao_QuandoExisteProxima()
    {
        // Arrange
        var (db, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Azul", Ordem = 2 }, 13);
        var cliente = CriarCliente(clienteService, servico.Id);

        // Act
        var resultado = clienteGraduacaoService.Graduar(cliente.Id, new GraduarClienteRequest { IdServico = servico.Id }, idUsuario: 7, idEmpresa: 13);

        // Assert
        Assert.Equal("Azul", resultado.NomeGraduacaoAtual);
        var historico = Assert.Single(db.HistoricoGraduacoes);
        Assert.Equal(7, historico.ConfirmadoPor);
        Assert.Equal(cliente.Id, historico.IdCliente);
    }

    [Fact]
    public void Graduar_DeveLancarExcecao_QuandoJaEstaNaUltimaGraduacao()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Preta", Ordem = 1 }, 13);
        var cliente = CriarCliente(clienteService, servico.Id);

        // Act
        var ex = Assert.Throws<AppException>(() =>
            clienteGraduacaoService.Graduar(cliente.Id, new GraduarClienteRequest { IdServico = servico.Id }, idUsuario: 7, idEmpresa: 13));

        // Assert
        Assert.Equal("Cliente já está na última graduação cadastrada nessa modalidade.", ex.Message);
    }

    [Fact]
    public void Graduar_DeveLancarExcecao_QuandoClienteSemEstadoInicial()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, _, servico) = CriarCenario();
        var cliente = CriarCliente(clienteService, servico.Id);

        // Act
        var ex = Assert.Throws<AppException>(() =>
            clienteGraduacaoService.Graduar(cliente.Id, new GraduarClienteRequest { IdServico = servico.Id }, idUsuario: 7, idEmpresa: 13));

        // Assert
        Assert.Equal("Cliente ainda não tem graduação inicial definida nessa modalidade.", ex.Message);
    }

    [Fact]
    public void ListarProgressoGraduacao_DeveMarcarApto100Porcento_QuandoRequisitosZerados()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Azul", Ordem = 2 }, 13);
        var cliente = CriarCliente(clienteService, servico.Id);

        // Act
        var lista = clienteGraduacaoService.ListarProgressoGraduacao(servico.Id, 13);

        // Assert
        Assert.Single(lista);
        Assert.Equal(cliente.Id, lista[0].IdCliente);
        Assert.True(lista[0].Apto);
        Assert.Equal(100, lista[0].ProgressoPercentual);
    }

    [Fact]
    public void ListarProgressoGraduacao_DeveIncluirNaoApto_ComProgressoParcial()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Azul", Ordem = 2, RequisitoDias = 180 }, 13);
        var cliente = CriarCliente(clienteService, servico.Id);

        // Act
        var lista = clienteGraduacaoService.ListarProgressoGraduacao(servico.Id, 13);

        // Assert
        Assert.Single(lista);
        Assert.Equal(cliente.Id, lista[0].IdCliente);
        Assert.False(lista[0].Apto);
        Assert.Equal(0, lista[0].ProgressoPercentual);
    }

    [Fact]
    public void ListarProgressoGraduacao_NaoDeveIncluirCliente_QuandoInativo()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Azul", Ordem = 2 }, 13);
        var cliente = CriarCliente(clienteService, servico.Id);
        clienteService.AlterarStatus(cliente.Id, false, 13);

        // Act
        var lista = clienteGraduacaoService.ListarProgressoGraduacao(servico.Id, 13);

        // Assert
        Assert.Empty(lista);
    }

    [Fact]
    public void ListarProgressoGraduacao_NaoDeveIncluirCliente_QuandoJaNaUltimaGraduacao()
    {
        // Arrange
        var (_, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Preta", Ordem = 1 }, 13);
        CriarCliente(clienteService, servico.Id);

        // Act
        var lista = clienteGraduacaoService.ListarProgressoGraduacao(servico.Id, 13);

        // Assert
        Assert.Empty(lista);
    }

    [Fact]
    public void ListarProgressoGraduacao_DeveContarPresencasDaModalidadeViaTurma_EOrdenarPorProgresso()
    {
        // Arrange
        var (db, clienteGraduacaoService, clienteService, graduacaoService, servico) = CriarCenario();
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);
        graduacaoService.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Azul", Ordem = 2, RequisitoPresencas = 2 }, 13);
        var clienteSemPresenca = CriarCliente(clienteService, servico.Id);
        var clienteComPresenca = clienteService.Criar(new CriarClienteRequest
        {
            Nome = "Aluno Frequente",
            Telefone = "79999991111",
            DataVencimento = new DateOnly(2026, 12, 31),
            IdServicos = new[] { servico.Id }
        }, 13);

        var turma = new TurmaService(db).Criar(new CriarTurmaRequest
        {
            IdServico = servico.Id,
            DiaSemana = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(20, 0)
        }, 13);

        // Act
        new PresencaService(db).LancarChamada(turma.Id, new LancarChamadaRequest
        {
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            Itens = new List<ItemLancamentoChamada>
            {
                new() { IdCliente = clienteComPresenca.Id, Status = StatusPresenca.Presente }
            }
        }, idUsuario: 1, idEmpresa: 13);

        // Assert
        var lista = clienteGraduacaoService.ListarProgressoGraduacao(servico.Id, 13);
        Assert.Equal(2, lista.Count);
        // quem tem presença fica com mais progresso e vem primeiro
        Assert.Equal(clienteComPresenca.Id, lista[0].IdCliente);
        Assert.Equal(1, lista[0].PresencasDesdeUltimaGraduacao);
        Assert.Equal(50, lista[0].ProgressoPercentual);
        Assert.Equal(clienteSemPresenca.Id, lista[1].IdCliente);
        Assert.Equal(0, lista[1].ProgressoPercentual);
    }
}
