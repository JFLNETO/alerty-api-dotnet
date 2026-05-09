public class ClienteService
{
    private readonly AppDbContext _db;

    public ClienteService(AppDbContext db)
    {
        _db = db;
    }

    public List<Cliente> Listar(
        int? idEmpresa,
        string? nome,
        bool? ativo)
    {
        var query = _db.Clientes.AsQueryable();

        if (idEmpresa.HasValue)
            query = query.Where(c => c.IdEmpresa == idEmpresa.Value);

        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(c =>
                c.Nome != null &&
                c.Nome.ToLower().Contains(nome.ToLower()));

        if (ativo.HasValue)
            query = query.Where(c => c.Ativo == ativo.Value);

        return query
            .OrderBy(c => c.DataVencimento)
            .ToList();
    }

    public Cliente? BuscarPorId(int id)
    {
        return _db.Clientes.FirstOrDefault(c => c.Id == id);
    }

 public Cliente Criar(CriarClienteRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Nome))
        throw new Exception("Nome é obrigatório.");

    if (string.IsNullOrWhiteSpace(request.Telefone))
        throw new Exception("Telefone é obrigatório.");

    if (request.DataVencimento == default)
        throw new Exception("Data de vencimento é obrigatória.");

    if (request.IdServicos is null || request.IdServicos.Length == 0)
        throw new Exception("Informe pelo menos um serviço/modalidade.");

    var cliente = new Cliente
    {
        Nome = request.Nome,
        Telefone = request.Telefone,
        DataVencimento = request.DataVencimento,
        IdServicos = request.IdServicos,
        IdEmpresa = request.IdEmpresa,
        UrlFoto = request.UrlFoto,
        Ativo = true,
        Selos = Array.Empty<int>()
    };

    cliente.IdCliente =
        $"{cliente.IdEmpresa}_55{cliente.Telefone}@s.whatsapp.net";

    _db.Clientes.Add(cliente);
    _db.SaveChanges();

    return cliente;
}

    public Cliente Atualizar(int id, AtualizarClienteRequest request)
    {
        var cliente = _db.Clientes.FirstOrDefault(c => c.Id == id);

        if (cliente is null)
            throw new Exception("Cliente não encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Nome))
            cliente.Nome = request.Nome;

        if (!string.IsNullOrWhiteSpace(request.Telefone))
            cliente.Telefone = request.Telefone;

        if (request.DataVencimento != default)
            cliente.DataVencimento = request.DataVencimento;

        if (!string.IsNullOrWhiteSpace(request.UrlFoto))
            cliente.UrlFoto = request.UrlFoto;

        if (request.IdEmpresa > 0)
            cliente.IdEmpresa = request.IdEmpresa;

        if (request.IdServicos is not null)
            cliente.IdServicos = request.IdServicos;

        cliente.IdCliente =
            $"{cliente.IdEmpresa}_55{cliente.Telefone}@s.whatsapp.net";

        _db.SaveChanges();

        return cliente;
    }

    public Cliente AlterarStatus(int id, bool ativo)
    {
        var cliente = _db.Clientes.FirstOrDefault(c => c.Id == id);

        if (cliente is null)
            throw new Exception("Cliente não encontrado.");

        cliente.Ativo = ativo;

        _db.SaveChanges();

        return cliente;
    }
}