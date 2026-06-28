public class ClienteServiceTests
{
    // ---------------------------------------------------------------
    // CRIAR
    // ---------------------------------------------------------------

    [Fact]
    public void Criar_DeveSalvarCliente_QuandoDadosValidos()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Telefone = "79999990000",
            DataVencimento = new DateOnly(2025, 12, 31),
            IdServicos = new[] { 1 },
            IdEmpresa = 13
        };

        // Act
        var resultado = service.Criar(request);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("João Silva", resultado.Nome);
        Assert.True(resultado.Ativo);
        Assert.Equal(13, resultado.IdEmpresa);
    }

    [Fact]
    public void Criar_DeveGerarIdClienteNoFormatoWhatsApp()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        var request = new CriarClienteRequest
        {
            Nome = "Maria Souza",
            Telefone = "79988887777",
            DataVencimento = new DateOnly(2025, 11, 15),
            IdServicos = new[] { 2 },
            IdEmpresa = 13
        };

        // Act
        var resultado = service.Criar(request);

        // Assert
        // Formato esperado: {idEmpresa}_55{telefone}@s.whatsapp.net
        Assert.Equal("13_5579988887777@s.whatsapp.net", resultado.IdCliente);
    }

    [Fact]
    public void Criar_DeveComecarComSelosVazios()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        var request = new CriarClienteRequest
        {
            Nome = "Pedro Costa",
            Telefone = "79911112222",
            DataVencimento = new DateOnly(2025, 10, 10),
            IdServicos = new[] { 1, 2 },
            IdEmpresa = 13
        };

        // Act
        var resultado = service.Criar(request);

        // Assert
        Assert.NotNull(resultado.Selos);
        Assert.Empty(resultado.Selos);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoNomeVazio()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        var request = new CriarClienteRequest
        {
            Nome = "",
            Telefone = "79999990000",
            DataVencimento = new DateOnly(2025, 12, 31),
            IdServicos = new[] { 1 },
            IdEmpresa = 13
        };

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => service.Criar(request));
        Assert.Equal("Nome é obrigatório.", ex.Message);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoTelefoneVazio()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Telefone = "",
            DataVencimento = new DateOnly(2025, 12, 31),
            IdServicos = new[] { 1 },
            IdEmpresa = 13
        };

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => service.Criar(request));
        Assert.Equal("Telefone é obrigatório.", ex.Message);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoDataVencimentoNaoInformada()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Telefone = "79999990000",
            DataVencimento = default, // DateOnly vazia
            IdServicos = new[] { 1 },
            IdEmpresa = 13
        };

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => service.Criar(request));
        Assert.Equal("Data de vencimento é obrigatória.", ex.Message);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoSemServicos()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Telefone = "79999990000",
            DataVencimento = new DateOnly(2025, 12, 31),
            IdServicos = Array.Empty<int>(), // sem serviços
            IdEmpresa = 13
        };

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => service.Criar(request));
        Assert.Equal("Informe pelo menos um serviço/modalidade.", ex.Message);
    }

    // ---------------------------------------------------------------
    // LISTAR
    // ---------------------------------------------------------------

    [Fact]
    public void Listar_DeveRetornarTodosClientes_QuandoSemFiltros()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.AddRange(
            new Cliente { Nome = "Ana", Telefone = "111", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 10) },
            new Cliente { Nome = "Bruno", Telefone = "222", IdEmpresa = 13, Ativo = false, DataVencimento = new DateOnly(2025, 1, 5) }
        );
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act
        var resultado = service.Listar(null, null, null);

        // Assert
        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public void Listar_DeveRetornarApenasClientesDaEmpresa_QuandoFiltrarPorIdEmpresa()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.AddRange(
            new Cliente { Nome = "Ana", Telefone = "111", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) },
            new Cliente { Nome = "Carlos", Telefone = "333", IdEmpresa = 99, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) }
        );
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act
        var resultado = service.Listar(idEmpresa: 13, null, null);

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Ana", resultado[0].Nome);
    }

    [Fact]
    public void Listar_DeveRetornarApenasAtivos_QuandoFiltrarPorAtivo()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.AddRange(
            new Cliente { Nome = "Ativo", Telefone = "111", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) },
            new Cliente { Nome = "Inativo", Telefone = "222", IdEmpresa = 13, Ativo = false, DataVencimento = new DateOnly(2025, 1, 1) }
        );
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act
        var resultado = service.Listar(null, null, ativo: true);

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Ativo", resultado[0].Nome);
    }

    [Fact]
    public void Listar_DeveFiltrarPorNomeParcial_CaseInsensitive()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.AddRange(
            new Cliente { Nome = "Fernanda Lima", Telefone = "111", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) },
            new Cliente { Nome = "Roberto Carlos", Telefone = "222", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) }
        );
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act — busca em minúsculo, nome tem maiúscula
        var resultado = service.Listar(null, "fernanda", null);

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Fernanda Lima", resultado[0].Nome);
    }

    [Fact]
    public void Listar_DeveOrdenarPorDataVencimento()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.AddRange(
            new Cliente { Nome = "C", Telefone = "333", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 3, 1) },
            new Cliente { Nome = "A", Telefone = "111", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) },
            new Cliente { Nome = "B", Telefone = "222", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 2, 1) }
        );
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act
        var resultado = service.Listar(null, null, null);

        // Assert — deve vir A, B, C pela data
        Assert.Equal("A", resultado[0].Nome);
        Assert.Equal("B", resultado[1].Nome);
        Assert.Equal("C", resultado[2].Nome);
    }

    [Fact]
    public void Listar_DeveFiltrarPorEmpresaEAtivoSimultaneamente()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.AddRange(
            new Cliente { Nome = "Ativo Empresa 13",   Telefone = "111", IdEmpresa = 13, Ativo = true,  DataVencimento = new DateOnly(2025, 1, 1) },
            new Cliente { Nome = "Inativo Empresa 13", Telefone = "222", IdEmpresa = 13, Ativo = false, DataVencimento = new DateOnly(2025, 1, 1) },
            new Cliente { Nome = "Ativo Empresa 99",   Telefone = "333", IdEmpresa = 99, Ativo = true,  DataVencimento = new DateOnly(2025, 1, 1) }
        );
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act — filtra empresa 13 E apenas ativos
        var resultado = service.Listar(idEmpresa: 13, null, ativo: true);

        // Assert — só deve retornar "Ativo Empresa 13"
        Assert.Single(resultado);
        Assert.Equal("Ativo Empresa 13", resultado[0].Nome);
    }

    [Fact]
    public void Listar_DeveFiltrarPorNomeEEmpresaSimultaneamente()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.AddRange(
            new Cliente { Nome = "Carlos Empresa 13", Telefone = "111", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) },
            new Cliente { Nome = "Carlos Empresa 99", Telefone = "222", IdEmpresa = 99, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) },
            new Cliente { Nome = "Ana Empresa 13",    Telefone = "333", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) }
        );
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act — busca "carlos" na empresa 13
        var resultado = service.Listar(idEmpresa: 13, "carlos", null);

        // Assert — só deve retornar o Carlos da empresa 13
        Assert.Single(resultado);
        Assert.Equal("Carlos Empresa 13", resultado[0].Nome);
    }

    // ---------------------------------------------------------------
    // BUSCAR POR ID
    // ---------------------------------------------------------------

    [Fact]
    public void BuscarPorId_DeveRetornarCliente_QuandoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente { Id = 1, Nome = "Ana", Telefone = "111", IdEmpresa = 13, Ativo = true, DataVencimento = new DateOnly(2025, 1, 1) });
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act
        var resultado = service.BuscarPorId(1, 13);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Ana", resultado.Nome);
    }

    [Fact]
    public void BuscarPorId_DeveRetornarNull_QuandoNaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        // Act
        var resultado = service.BuscarPorId(999, 13);

        // Assert
        Assert.Null(resultado);
    }

    // ---------------------------------------------------------------
    // ATUALIZAR
    // ---------------------------------------------------------------

    [Fact]
    public void Atualizar_DeveAtualizarDados_QuandoClienteExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Nome Antigo",
            Telefone = "79900000000",
            IdEmpresa = 13,
            Ativo = true,
            DataVencimento = new DateOnly(2025, 1, 1)
        });
        db.SaveChanges();

        var service = new ClienteService(db);

        var request = new AtualizarClienteRequest
        {
            Nome = "Nome Novo",
            Telefone = "79911111111",
            DataVencimento = new DateOnly(2025, 12, 31),
            IdServicos = new[] { 3 },
            IdEmpresa = 13
        };

        // Act
        var resultado = service.Atualizar(1, request, 13);

        // Assert
        Assert.Equal("Nome Novo", resultado.Nome);
        Assert.Equal("79911111111", resultado.Telefone);
        Assert.Equal(new DateOnly(2025, 12, 31), resultado.DataVencimento);
    }

    [Fact]
    public void Atualizar_DeveAtualizarIdCliente_QuandoTelefoneAtualizado()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1,
            Nome = "Ana",
            Telefone = "79900000000",
            IdEmpresa = 13,
            Ativo = true,
            DataVencimento = new DateOnly(2025, 1, 1)
        });
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act
        var resultado = service.Atualizar(1, new AtualizarClienteRequest
        {
            Telefone = "79988889999",
            DataVencimento = new DateOnly(2025, 6, 1),
            IdEmpresa = 13
        }, 13);

        // Assert
        Assert.Equal("13_5579988889999@s.whatsapp.net", resultado.IdCliente);
    }

    [Fact]
    public void Atualizar_DeveLancarExcecao_QuandoClienteNaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        // Act & Assert
        var ex = Assert.Throws<Exception>(() =>
            service.Atualizar(999, new AtualizarClienteRequest
            {
                Nome = "Qualquer",
                IdEmpresa = 13,
                DataVencimento = new DateOnly(2025, 1, 1)
            }, 13));

        Assert.Equal("Cliente não encontrado.", ex.Message);
    }

    // ---------------------------------------------------------------
    // ALTERAR STATUS
    // ---------------------------------------------------------------

    [Fact]
    public void AlterarStatus_DeveInativarCliente_QuandoAtivoEhFalse()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1, Nome = "Ativo", Telefone = "111",
            IdEmpresa = 13, Ativo = true,
            DataVencimento = new DateOnly(2025, 1, 1)
        });
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act
        var resultado = service.AlterarStatus(1, false, 13);

        // Assert
        Assert.False(resultado.Ativo);
    }

    [Fact]
    public void AlterarStatus_DeveReativarCliente_QuandoAtivoEhTrue()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        db.Clientes.Add(new Cliente
        {
            Id = 1, Nome = "Inativo", Telefone = "111",
            IdEmpresa = 13, Ativo = false,
            DataVencimento = new DateOnly(2025, 1, 1)
        });
        db.SaveChanges();

        var service = new ClienteService(db);

        // Act
        var resultado = service.AlterarStatus(1, true, 13);

        // Assert
        Assert.True(resultado.Ativo);
    }

    [Fact]
    public void AlterarStatus_DeveLancarExcecao_QuandoClienteNaoExiste()
    {
        // Arrange
        var db = DbContextFactory.Criar();
        var service = new ClienteService(db);

        // Act & Assert
        var ex = Assert.Throws<Exception>(() => service.AlterarStatus(999, false, 13));
        Assert.Equal("Cliente não encontrado.", ex.Message);
    }
}