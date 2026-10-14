public class GraduacaoServiceTests
{
    [Fact]
    public void Criar_DeveSalvarGraduacao_QuandoDadosValidos()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        var service = new GraduacaoService(db);

        // Act
        var resultado = service.Criar(new CriarGraduacaoRequest
        {
            IdServico = servico.Id,
            Nome = "Branca",
            Ordem = 1,
            RequisitoDias = 180,
            RequisitoPresencas = 80
        }, 13);

        // Assert
        Assert.Equal("Branca", resultado.Nome);
        Assert.Equal(1, resultado.Ordem);
        Assert.Equal(13, resultado.IdEmpresa);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoServicoNaoExisteNaEmpresa()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 99);
        var service = new GraduacaoService(db);

        // Act
        var ex = Assert.Throws<AppException>(() => service.Criar(new CriarGraduacaoRequest
        {
            IdServico = servico.Id,
            Nome = "Branca",
            Ordem = 1
        }, 13));

        // Assert
        Assert.Equal("Modalidade não encontrada.", ex.Message);
    }

    [Fact]
    public void Listar_DeveRetornarOrdenadoPorOrdem()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        var service = new GraduacaoService(db);
        service.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Azul", Ordem = 2 }, 13);
        service.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);

        // Act
        var resultado = service.Listar(servico.Id, 13);

        // Assert
        Assert.Equal("Branca", resultado[0].Nome);
        Assert.Equal("Azul", resultado[1].Nome);
    }

    [Fact]
    public void Atualizar_DeveAtualizarRequisitos_QuandoGraduacaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        var service = new GraduacaoService(db);
        var criada = service.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);

        // Act
        var resultado = service.Atualizar(criada.Id, new AtualizarGraduacaoRequest
        {
            RequisitoDias = 180,
            RequisitoPresencas = 80
        }, 13);

        // Assert
        Assert.Equal(180, resultado.RequisitoDias);
        Assert.Equal(80, resultado.RequisitoPresencas);
    }

    [Fact]
    public void Deletar_DeveLancarExcecao_QuandoNaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new GraduacaoService(db);

        // Act
        var ex = Assert.Throws<AppException>(() => service.Deletar(999, 13));

        // Assert
        Assert.Equal("Graduação não encontrada.", ex.Message);
    }

    [Fact]
    public void CriarModeloPadraoJiuJitsu_DeveCriarCincoFaixas_QuandoModalidadeSemGraduacoes()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        var service = new GraduacaoService(db);

        // Act
        var resultado = service.CriarModeloPadraoJiuJitsu(servico.Id, 13);

        // Assert
        Assert.Equal(5, resultado.Count);
        Assert.Equal("Branca", resultado[0].Nome);
        Assert.Equal("Preta", resultado[4].Nome);
        Assert.All(resultado, g => Assert.Equal(0, g.RequisitoDias));
    }

    [Fact]
    public void CriarModeloPadraoJiuJitsu_DeveLancarExcecao_QuandoJaTemGraduacoes()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var servico = new ServicoService(db).Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        var service = new GraduacaoService(db);
        service.Criar(new CriarGraduacaoRequest { IdServico = servico.Id, Nome = "Branca", Ordem = 1 }, 13);

        // Act
        var ex = Assert.Throws<AppException>(() => service.CriarModeloPadraoJiuJitsu(servico.Id, 13));

        // Assert
        Assert.Equal("Essa modalidade já tem graduações cadastradas.", ex.Message);
    }
}
