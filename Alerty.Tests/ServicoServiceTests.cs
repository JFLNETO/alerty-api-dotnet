public class ServicoServiceTests
{
    [Fact]
    public void Criar_DeveSalvarServico_QuandoDadosValidos()
    {
        var db = DbContextFactory.Criar();
        var service = new ServicoService(db);

        var resultado = service.Criar(new CriarServicoRequest
        {
            Nome = "Jiu-Jitsu",
            Valor = 150m,
            RecorrenciaValor = 1,
            RecorrenciaTipo = "mensal"
        }, 13);

        Assert.Equal("Jiu-Jitsu", resultado.Nome);
        Assert.Equal(13, resultado.IdEmpresa);
        Assert.Equal(150m, resultado.Valor);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoNomeVazio()
    {
        var db = DbContextFactory.Criar();
        var service = new ServicoService(db);

        var ex = Assert.Throws<AppException>(() => service.Criar(new CriarServicoRequest
        {
            Nome = ""
        }, 13));

        Assert.Equal("Nome é obrigatório.", ex.Message);
    }

    [Fact]
    public void Listar_DeveRetornarApenasServicosDaEmpresa()
    {
        var db = DbContextFactory.Criar();
        var service = new ServicoService(db);

        service.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);
        service.Criar(new CriarServicoRequest { Nome = "Muay Thai" }, 99);

        var resultado = service.Listar(13);

        Assert.Single(resultado);
        Assert.Equal("Jiu-Jitsu", resultado[0].Nome);
    }

    [Fact]
    public void Atualizar_DeveAtualizarDados_QuandoServicoExiste()
    {
        var db = DbContextFactory.Criar();
        var service = new ServicoService(db);
        var criado = service.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu", Valor = 150m }, 13);

        var resultado = service.Atualizar(criado.Id, new AtualizarServicoRequest
        {
            Nome = "Jiu-Jitsu Adulto",
            Valor = 180m
        }, 13);

        Assert.Equal("Jiu-Jitsu Adulto", resultado.Nome);
        Assert.Equal(180m, resultado.Valor);
    }

    [Fact]
    public void Atualizar_DeveLancarExcecao_QuandoServicoNaoExiste()
    {
        var db = DbContextFactory.Criar();
        var service = new ServicoService(db);

        var ex = Assert.Throws<AppException>(() => service.Atualizar(999, new AtualizarServicoRequest
        {
            Nome = "Qualquer"
        }, 13));

        Assert.Equal("Modalidade não encontrada.", ex.Message);
    }

    [Fact]
    public void Deletar_DeveRemoverServico_QuandoExiste()
    {
        var db = DbContextFactory.Criar();
        var service = new ServicoService(db);
        var criado = service.Criar(new CriarServicoRequest { Nome = "Jiu-Jitsu" }, 13);

        service.Deletar(criado.Id, 13);

        Assert.Empty(service.Listar(13));
    }

    [Fact]
    public void Deletar_DeveLancarExcecao_QuandoNaoExiste()
    {
        var db = DbContextFactory.Criar();
        var service = new ServicoService(db);

        var ex = Assert.Throws<AppException>(() => service.Deletar(999, 13));
        Assert.Equal("Modalidade não encontrada.", ex.Message);
    }
}
