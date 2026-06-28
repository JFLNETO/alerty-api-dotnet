

public class PagamentoServiceTests
{
    // ---------------------------------------------------------------
    // CONFIRMAR PAGAMENTO — CENÁRIOS DE SUCESSO
    // ---------------------------------------------------------------

    [Fact]
    public void ConfirmarPagamento_DeveAtualizarVencimento_QuandoClienteExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Carlos Mendes",
            Telefone = "79999990000",
            IdEmpresa = 13,
            Ativo = true,
            DataVencimento = new DateOnly(2025, 9, 10)
        });
        db.SaveChanges();

        var service = new PagamentoService(db);

        var request = new ConfirmarPagamentoRequest
        {
            ClienteId = 1,
            Valor = 150.00m,
            NovoVencimento = new DateOnly(2025, 10, 10)
        };

        // Act
        service.ConfirmarPagamento(request, 13);

        // Assert — verifica que o vencimento foi atualizado no banco
        var clienteAtualizado = db.Clientes.Find(1);
        Assert.Equal(new DateOnly(2025, 10, 10), clienteAtualizado!.DataVencimento);
    }

    [Fact]
    public void ConfirmarPagamento_DeveRegistrarDataUltimoPagamento()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Ana Paula",
            Telefone = "79988880000",
            IdEmpresa = 13,
            Ativo = true,
            DataVencimento = new DateOnly(2025, 9, 1),
            DataUltimoPagamento = null // ainda nunca pagou
        });
        db.SaveChanges();

        var service = new PagamentoService(db);

        var antes = DateTime.UtcNow.AddSeconds(-1);

        // Act
        service.ConfirmarPagamento(new ConfirmarPagamentoRequest
        {
            ClienteId = 1,
            Valor = 100m,
            NovoVencimento = new DateOnly(2025, 10, 1)
        }, 13);

        // Assert
        var clienteAtualizado = db.Clientes.Find(1);
        Assert.NotNull(clienteAtualizado!.DataUltimoPagamento);
        Assert.True(clienteAtualizado.DataUltimoPagamento >= antes);
    }

    [Fact]
    public void ConfirmarPagamento_DeveLimparSelos_AoConfirmarPagamento()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Marcos",
            Telefone = "79977770000",
            IdEmpresa = 13,
            Ativo = true,
            DataVencimento = new DateOnly(2025, 9, 5),
            Selos = new[] { 1, 2, 3 } // tinha selos de inadimplência
        });
        db.SaveChanges();

        var service = new PagamentoService(db);

        // Act
        service.ConfirmarPagamento(new ConfirmarPagamentoRequest
        {
            ClienteId = 1,
            Valor = 100m,
            NovoVencimento = new DateOnly(2025, 10, 5)
        }, 13);

        // Assert — selos devem ser limpos após pagamento
        var clienteAtualizado = db.Clientes.Find(1);
        Assert.NotNull(clienteAtualizado!.Selos);
        Assert.Empty(clienteAtualizado.Selos);
    }

    [Fact]
    public void ConfirmarPagamento_DeveCriarHistorico_ComDadosCorretos()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Julia Ramos",
            Telefone = "79966660000",
            IdEmpresa = 13,
            Ativo = true,
            DataVencimento = new DateOnly(2025, 9, 20)
        });
        db.SaveChanges();

        var service = new PagamentoService(db);

        // Act
        service.ConfirmarPagamento(new ConfirmarPagamentoRequest
        {
            ClienteId = 1,
            Valor = 200m,
            NovoVencimento = new DateOnly(2025, 10, 20)
        }, 13);

        // Assert — verifica histórico gerado
        var historico = db.HistoricoPagamentos.FirstOrDefault();
        Assert.NotNull(historico);
        Assert.Equal("1", historico!.ClienteId);
        Assert.Equal(13, historico.IdEmpresa);
        Assert.Equal(200m, historico.Valor);
        // O vencimento anterior deve ter sido salvo antes da atualização
        Assert.Equal(new DateOnly(2025, 9, 20), historico.DataVencimentoAnterior);
    }

    [Fact]
    public void ConfirmarPagamento_DevePreservarVencimentoAnterior_NoHistorico()
    {
        // Arrange — vencimento original antes do pagamento
        var vencimentoOriginal = new DateOnly(2025, 8, 15);

        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Pedro",
            Telefone = "79955550000",
            IdEmpresa = 13,
            Ativo = true,
            DataVencimento = vencimentoOriginal
        });
        db.SaveChanges();

        var service = new PagamentoService(db);

        // Act
        service.ConfirmarPagamento(new ConfirmarPagamentoRequest
        {
            ClienteId = 1,
            Valor = 100m,
            NovoVencimento = new DateOnly(2025, 9, 15) // novo vencimento
        }, 13);

        // Assert — histórico deve guardar o vencimento ANTES do pagamento
        var historico = db.HistoricoPagamentos.First();
        Assert.Equal(vencimentoOriginal, historico.DataVencimentoAnterior);

        // E o cliente deve ter o novo vencimento
        var cliente = db.Clientes.Find(1);
        Assert.Equal(new DateOnly(2025, 9, 15), cliente!.DataVencimento);
    }

    [Fact]
    public void ConfirmarPagamento_DeveRetornarMensagemSucesso()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Teste",
            Telefone = "79944440000",
            IdEmpresa = 13,
            Ativo = true,
            DataVencimento = new DateOnly(2025, 9, 1)
        });
        db.SaveChanges();

        var service = new PagamentoService(db);

        // Act
        var resultado = service.ConfirmarPagamento(new ConfirmarPagamentoRequest
        {
            ClienteId = 1,
            Valor = 100m,
            NovoVencimento = new DateOnly(2025, 10, 1)
        }, 13);

        // Assert — o retorno é um objeto anônimo, verificamos via reflexão
        Assert.NotNull(resultado);
        var mensagem = resultado.GetType().GetProperty("mensagem")?.GetValue(resultado)?.ToString();
        Assert.Equal("Pagamento confirmado com sucesso", mensagem);
    }

    // ---------------------------------------------------------------
    // CONFIRMAR PAGAMENTO — CENÁRIOS DE ERRO
    // ---------------------------------------------------------------

    [Fact]
    public void ConfirmarPagamento_DeveLancarExcecao_QuandoClienteNaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new PagamentoService(db);

        // Act & Assert
        var ex = Assert.Throws<Exception>(() =>
            service.ConfirmarPagamento(new ConfirmarPagamentoRequest
            {
                ClienteId = 999, // não existe
                Valor = 100m,
                NovoVencimento = new DateOnly(2025, 10, 1)
            }, 13));

        Assert.Equal("Cliente não encontrado.", ex.Message);
    }

    [Fact]
    public void ConfirmarPagamento_DeveRegistrarHistorico_ComDataPagamentoRecente()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1, Nome = "Teste", Telefone = "111",
            IdEmpresa = 13, Ativo = true,
            DataVencimento = new DateOnly(2025, 9, 1)
        });
        db.SaveChanges();

        var service = new PagamentoService(db);
        var antes = DateTime.UtcNow.AddSeconds(-1);

        // Act
        service.ConfirmarPagamento(new ConfirmarPagamentoRequest
        {
            ClienteId = 1,
            Valor = 100m,
            NovoVencimento = new DateOnly(2025, 10, 1)
        }, 13);

        // Assert
        var historico = db.HistoricoPagamentos.First();
        Assert.True(historico.DataPagamento >= antes);
        Assert.True(historico.CreatedDate >= antes);
    }
}