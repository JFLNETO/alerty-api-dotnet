public class PresencaServiceTests
{
    private static (AppDbContext db, Servico servico, Turma turma) CriarCenario(AppDbContext db, int idEmpresa = 13)
    {
        var servico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, idEmpresa);
        var turma = new TurmaService(db).Criar(new CriarTurmaRequest
        {
            IdServico = servico.Id,
            DiaSemana = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(20, 0)
        }, idEmpresa);

        return (db, servico, turma);
    }

    [Fact]
    public void BuscarChamada_DeveListarAlunosDaModalidade_ComMatriculadosPrimeiro()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var (_, servico, turma) = CriarCenario(db);
        var clienteService = new ClienteService(db, new ClienteGraduacaoService(db));

        var matriculado = clienteService.Criar(new CriarClienteRequest
        {
            Nome = "Ana",
            Telefone = "79999990001",
            DataVencimento = new DateOnly(2026, 12, 31),
            IdServicos = new[] { servico.Id }
        }, 13);
        clienteService.Atualizar(matriculado.Id, new AtualizarClienteRequest
        {
            IdTurmas = new[] { turma.Id }
        }, 13);

        clienteService.Criar(new CriarClienteRequest
        {
            Nome = "Bruno",
            Telefone = "79999990002",
            DataVencimento = new DateOnly(2026, 12, 31),
            IdServicos = new[] { servico.Id }
        }, 13);

        var service = new PresencaService(db);

        // Act
        var resultado = service.BuscarChamada(turma.Id, new DateOnly(2026, 9, 15), 13);

        // Assert
        Assert.Equal(2, resultado.Count);
        Assert.Equal("Ana", resultado[0].Nome);
        Assert.True(resultado[0].MatriculadoNestaTurma);
        Assert.False(resultado[1].MatriculadoNestaTurma);
        Assert.All(resultado, a => Assert.Null(a.Status));
    }

    [Fact]
    public void BuscarChamada_DeveLancarExcecao_QuandoTurmaNaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new PresencaService(db);

        // Act
        var ex = Assert.Throws<AppException>(() => service.BuscarChamada(999, new DateOnly(2026, 9, 15), 13));

        // Assert
        Assert.Equal("Turma não encontrada.", ex.Message);
    }

    [Fact]
    public void LancarChamada_DeveCriarPresencas_QuandoDadosValidos()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var (_, servico, turma) = CriarCenario(db);
        var cliente = new ClienteService(db, new ClienteGraduacaoService(db)).Criar(new CriarClienteRequest
        {
            Nome = "Ana",
            Telefone = "79999990001",
            DataVencimento = new DateOnly(2026, 12, 31),
            IdServicos = new[] { servico.Id }
        }, 13);

        var service = new PresencaService(db);

        // Act
        var resultado = service.LancarChamada(turma.Id, new LancarChamadaRequest
        {
            Data = new DateOnly(2026, 9, 15),
            Itens = new List<ItemLancamentoChamada>
            {
                new() { IdCliente = cliente.Id, Status = StatusPresenca.Presente }
            }
        }, idUsuario: 1, idEmpresa: 13);

        // Assert
        Assert.Single(resultado);
        Assert.Equal(StatusPresenca.Presente, resultado[0].Status);
        Assert.Equal(1, resultado[0].LancadoPor);
    }

    [Fact]
    public void LancarChamada_DeveAtualizarPresencaExistente_QuandoLancadaDeNovoParaMesmaData()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var (_, servico, turma) = CriarCenario(db);
        var cliente = new ClienteService(db, new ClienteGraduacaoService(db)).Criar(new CriarClienteRequest
        {
            Nome = "Ana",
            Telefone = "79999990001",
            DataVencimento = new DateOnly(2026, 12, 31),
            IdServicos = new[] { servico.Id }
        }, 13);
        var service = new PresencaService(db);
        var data = new DateOnly(2026, 9, 15);

        service.LancarChamada(turma.Id, new LancarChamadaRequest
        {
            Data = data,
            Itens = new List<ItemLancamentoChamada>
            {
                new() { IdCliente = cliente.Id, Status = StatusPresenca.Ausente }
            }
        }, idUsuario: 1, idEmpresa: 13);

        // Act
        service.LancarChamada(turma.Id, new LancarChamadaRequest
        {
            Data = data,
            Itens = new List<ItemLancamentoChamada>
            {
                new() { IdCliente = cliente.Id, Status = StatusPresenca.Presente }
            }
        }, idUsuario: 2, idEmpresa: 13);

        // Assert
        var chamada = service.BuscarChamada(turma.Id, data, 13);
        Assert.Single(chamada);
        Assert.Equal(StatusPresenca.Presente, chamada[0].Status);
    }

    [Fact]
    public void LancarChamada_DeveLancarExcecao_QuandoTurmaNaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new PresencaService(db);

        // Act
        var ex = Assert.Throws<AppException>(() => service.LancarChamada(999, new LancarChamadaRequest
        {
            Data = new DateOnly(2026, 9, 15),
            Itens = new List<ItemLancamentoChamada> { new() { IdCliente = 1, Status = StatusPresenca.Presente } }
        }, idUsuario: 1, idEmpresa: 13));

        // Assert
        Assert.Equal("Turma não encontrada.", ex.Message);
    }

    [Fact]
    public void LancarChamada_DeveLancarExcecao_QuandoNenhumItemInformado()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var (_, _, turma) = CriarCenario(db);
        var service = new PresencaService(db);

        // Act
        var ex = Assert.Throws<AppException>(() => service.LancarChamada(turma.Id, new LancarChamadaRequest
        {
            Data = new DateOnly(2026, 9, 15),
            Itens = new List<ItemLancamentoChamada>()
        }, idUsuario: 1, idEmpresa: 13));

        // Assert
        Assert.Equal("Informe ao menos um aluno na chamada.", ex.Message);
    }
}
