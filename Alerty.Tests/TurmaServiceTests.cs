public class TurmaServiceTests
{
    [Fact]
    public void Criar_DeveSalvarTurma_QuandoDadosValidos()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servicoService = new ServicoService(db);
        var service = new TurmaService(db);
        var servico = servicoService.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);

        // Act
        var resultado = service.Criar(new CriarTurmaRequest
        {
            IdServico = servico.Id,
            Nome = "Jiu-Jitsu Ter/Qui 19h",
            DiaSemana = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(20, 0)
        }, 13);

        // Assert
        Assert.Equal("Jiu-Jitsu Ter/Qui 19h", resultado.Nome);
        Assert.Equal(13, resultado.IdEmpresa);
        Assert.True(resultado.Ativo);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoServicoNaoExisteNaEmpresa()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servicoService = new ServicoService(db);
        var service = new TurmaService(db);
        var servico = servicoService.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 99);

        // Act
        var ex = Assert.Throws<AppException>(() => service.Criar(new CriarTurmaRequest
        {
            IdServico = servico.Id,
            DiaSemana = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(20, 0)
        }, 13));

        // Assert
        Assert.Equal("Modalidade não encontrada.", ex.Message);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoHoraFimNaoDepoisDeHoraInicio()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servicoService = new ServicoService(db);
        var service = new TurmaService(db);
        var servico = servicoService.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);

        // Act
        var ex = Assert.Throws<AppException>(() => service.Criar(new CriarTurmaRequest
        {
            IdServico = servico.Id,
            DiaSemana = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(18, 0)
        }, 13));

        // Assert
        Assert.Equal("Horário de fim deve ser depois do horário de início.", ex.Message);
    }

    [Fact]
    public void Listar_DeveRetornarApenasTurmasDaEmpresa()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servicoService = new ServicoService(db);
        var service = new TurmaService(db);
        var servicoA = servicoService.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        var servicoB = servicoService.Criar(new CriarServicoRequest { Nome = "Muay Thai" }, 99);

        service.Criar(new CriarTurmaRequest
        {
            IdServico = servicoA.Id,
            DiaSemana = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(20, 0)
        }, 13);
        service.Criar(new CriarTurmaRequest
        {
            IdServico = servicoB.Id,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(20, 0)
        }, 99);

        // Act
        var resultado = service.Listar(13);

        // Assert
        Assert.Single(resultado);
    }

    [Fact]
    public void Atualizar_DeveAtualizarDados_QuandoTurmaExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servicoService = new ServicoService(db);
        var service = new TurmaService(db);
        var servico = servicoService.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        var criada = service.Criar(new CriarTurmaRequest
        {
            IdServico = servico.Id,
            DiaSemana = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(20, 0)
        }, 13);

        // Act
        var resultado = service.Atualizar(criada.Id, new AtualizarTurmaRequest
        {
            Nome = "Jiu-Jitsu Avançado",
            Ativo = false
        }, 13);

        // Assert
        Assert.Equal("Jiu-Jitsu Avançado", resultado.Nome);
        Assert.False(resultado.Ativo);
    }

    [Fact]
    public void Atualizar_DeveLancarExcecao_QuandoTurmaNaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new TurmaService(db);

        // Act
        var ex = Assert.Throws<AppException>(() => service.Atualizar(999, new AtualizarTurmaRequest
        {
            Nome = "Qualquer"
        }, 13));

        // Assert
        Assert.Equal("Turma não encontrada.", ex.Message);
    }

    [Fact]
    public void Deletar_DeveRemoverTurma_QuandoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servicoService = new ServicoService(db);
        var service = new TurmaService(db);
        var servico = servicoService.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        var criada = service.Criar(new CriarTurmaRequest
        {
            IdServico = servico.Id,
            DiaSemana = DayOfWeek.Tuesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFim = new TimeOnly(20, 0)
        }, 13);

        // Act
        service.Deletar(criada.Id, 13);

        // Assert
        Assert.Empty(service.Listar(13));
    }
}
