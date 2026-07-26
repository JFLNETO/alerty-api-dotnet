public class RegraAlertaServiceTests
{
    private static void SeedEmpresa(AppDbContext db, int idEmpresa, Plano plano)
    {
        db.ConfigEmpresas.Add(new ConfigEmpresa { IdEmpresa = idEmpresa, Plano = plano });
        db.SaveChanges();
    }

    // ---------------------------------------------------------------
    // CRIAR
    // ---------------------------------------------------------------

    [Fact]
    public void Criar_DeveSalvarRegra_QuandoDadosValidos()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico);
        var service = new RegraAlertaService(db);

        var resultado = service.Criar(new CriarRegraAlertaRequest
        {
            Tipo = TipoAlerta.AntesVencimento,
            DiasOffset = 2,
            Mensagem = "Olá {nome}, sua mensalidade vence em breve."
        }, 13);

        Assert.True(resultado.Ativo);
        Assert.Equal(2, resultado.DiasOffset);
    }

    [Fact]
    public void Criar_DeveZerarDiasOffset_QuandoTipoNoDia()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico);
        var service = new RegraAlertaService(db);

        var resultado = service.Criar(new CriarRegraAlertaRequest
        {
            Tipo = TipoAlerta.NoDia,
            DiasOffset = 5,
            Mensagem = "Vence hoje!"
        }, 13);

        Assert.Equal(0, resultado.DiasOffset);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoMensagemVazia()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico);
        var service = new RegraAlertaService(db);

        var ex = Assert.Throws<AppException>(() => service.Criar(new CriarRegraAlertaRequest
        {
            Tipo = TipoAlerta.NoDia,
            Mensagem = ""
        }, 13));

        Assert.Equal("Mensagem é obrigatória.", ex.Message);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoDiasOffsetZeroEmTipoDiferenteDeNoDia()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico);
        var service = new RegraAlertaService(db);

        var ex = Assert.Throws<AppException>(() => service.Criar(new CriarRegraAlertaRequest
        {
            Tipo = TipoAlerta.AposVencimento,
            DiasOffset = 0,
            Mensagem = "Está atrasado."
        }, 13));

        Assert.Equal("Informe uma quantidade de dias maior que zero.", ex.Message);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoExcedeLimiteDoPlano()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico); // limite = 1
        var service = new RegraAlertaService(db);

        service.Criar(new CriarRegraAlertaRequest { Tipo = TipoAlerta.NoDia, Mensagem = "Vence hoje." }, 13);

        var ex = Assert.Throws<AppException>(() => service.Criar(new CriarRegraAlertaRequest
        {
            Tipo = TipoAlerta.AntesVencimento,
            DiasOffset = 1,
            Mensagem = "Vence amanhã."
        }, 13));

        Assert.Contains("plano atual permite", ex.Message);
    }

    [Fact]
    public void Criar_DevePermitirVariasRegras_QuandoPlanoPlus()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Plus); // limite = 5
        var service = new RegraAlertaService(db);

        for (var i = 1; i <= 5; i++)
        {
            service.Criar(new CriarRegraAlertaRequest
            {
                Tipo = TipoAlerta.AntesVencimento,
                DiasOffset = i,
                Mensagem = $"Faltam {i} dias."
            }, 13);
        }

        Assert.Equal(5, service.Listar(13).Count);
    }

    // ---------------------------------------------------------------
    // ATUALIZAR
    // ---------------------------------------------------------------

    [Fact]
    public void Atualizar_DeveAtualizarDados_QuandoRegraExiste()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico);
        var service = new RegraAlertaService(db);
        var criada = service.Criar(new CriarRegraAlertaRequest { Tipo = TipoAlerta.NoDia, Mensagem = "Vence hoje." }, 13);

        var resultado = service.Atualizar(criada.Id, new AtualizarRegraAlertaRequest
        {
            Tipo = TipoAlerta.AntesVencimento,
            DiasOffset = 3,
            Mensagem = "Faltam 3 dias."
        }, 13);

        Assert.Equal(TipoAlerta.AntesVencimento, resultado.Tipo);
        Assert.Equal(3, resultado.DiasOffset);
    }

    [Fact]
    public void Atualizar_DeveLancarExcecao_QuandoRegraNaoExiste()
    {
        var db = DbContextFactory.Criar();
        var service = new RegraAlertaService(db);

        var ex = Assert.Throws<AppException>(() => service.Atualizar(999, new AtualizarRegraAlertaRequest
        {
            Tipo = TipoAlerta.NoDia,
            Mensagem = "Qualquer"
        }, 13));

        Assert.Equal("Regra de alerta não encontrada.", ex.Message);
    }

    // ---------------------------------------------------------------
    // ALTERAR STATUS
    // ---------------------------------------------------------------

    [Fact]
    public void AlterarStatus_DevePermitirDesativar_MesmoNoLimiteDoPlano()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico);
        var service = new RegraAlertaService(db);
        var criada = service.Criar(new CriarRegraAlertaRequest { Tipo = TipoAlerta.NoDia, Mensagem = "Vence hoje." }, 13);

        var resultado = service.AlterarStatus(criada.Id, false, 13);

        Assert.False(resultado.Ativo);
    }

    [Fact]
    public void AlterarStatus_DeveLancarExcecao_QuandoReativarExcedeLimite()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico); // limite = 1
        var service = new RegraAlertaService(db);

        var regra1 = service.Criar(new CriarRegraAlertaRequest { Tipo = TipoAlerta.NoDia, Mensagem = "Vence hoje." }, 13);
        service.AlterarStatus(regra1.Id, false, 13);
        service.Criar(new CriarRegraAlertaRequest { Tipo = TipoAlerta.AntesVencimento, DiasOffset = 1, Mensagem = "Amanhã." }, 13);

        var ex = Assert.Throws<AppException>(() => service.AlterarStatus(regra1.Id, true, 13));

        Assert.Contains("plano atual permite", ex.Message);
    }

    // ---------------------------------------------------------------
    // DELETAR
    // ---------------------------------------------------------------

    [Fact]
    public void Deletar_DeveRemoverRegra_QuandoExiste()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Basico);
        var service = new RegraAlertaService(db);
        var criada = service.Criar(new CriarRegraAlertaRequest { Tipo = TipoAlerta.NoDia, Mensagem = "Vence hoje." }, 13);

        service.Deletar(criada.Id, 13);

        Assert.Empty(service.Listar(13));
    }

    [Fact]
    public void Deletar_DeveLancarExcecao_QuandoNaoExiste()
    {
        var db = DbContextFactory.Criar();
        var service = new RegraAlertaService(db);

        var ex = Assert.Throws<AppException>(() => service.Deletar(999, 13));
        Assert.Equal("Regra de alerta não encontrada.", ex.Message);
    }

    // ---------------------------------------------------------------
    // LISTAR
    // ---------------------------------------------------------------

    [Fact]
    public void Listar_DeveRetornarApenasRegrasDaEmpresa()
    {
        var db = DbContextFactory.Criar();
        SeedEmpresa(db, 13, Plano.Plus);
        SeedEmpresa(db, 99, Plano.Plus);
        var service = new RegraAlertaService(db);

        service.Criar(new CriarRegraAlertaRequest { Tipo = TipoAlerta.NoDia, Mensagem = "Empresa 13" }, 13);
        service.Criar(new CriarRegraAlertaRequest { Tipo = TipoAlerta.NoDia, Mensagem = "Empresa 99" }, 99);

        var resultado = service.Listar(13);

        Assert.Single(resultado);
        Assert.Equal("Empresa 13", resultado[0].Mensagem);
    }
}
