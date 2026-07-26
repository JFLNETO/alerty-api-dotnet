public class ServicoService
{
    private readonly AppDbContext _db;

    public ServicoService(AppDbContext db)
    {
        _db = db;
    }

    public List<Servico> Listar(int idEmpresa)
    {
        return _db.Servicos
            .Where(s => s.IdEmpresa == idEmpresa)
            .OrderBy(s => s.Nome)
            .ToList();
    }

    public Servico Criar(CriarServicoRequest request, int idEmpresa)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new AppException("Nome é obrigatório.");

        var servico = new Servico
        {
            Nome = request.Nome,
            IdEmpresa = idEmpresa,
            Valor = request.Valor,
            RecorrenciaValor = request.RecorrenciaValor,
            RecorrenciaTipo = request.RecorrenciaTipo,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _db.Servicos.Add(servico);
        _db.SaveChanges();

        return servico;
    }

    public Servico Atualizar(int id, AtualizarServicoRequest request, int idEmpresa)
    {
        var servico = _db.Servicos.FirstOrDefault(s => s.Id == id && s.IdEmpresa == idEmpresa)
            ?? throw new AppException("Modalidade não encontrada.");

        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new AppException("Nome é obrigatório.");

        servico.Nome = request.Nome;
        servico.Valor = request.Valor;
        servico.RecorrenciaValor = request.RecorrenciaValor;
        servico.RecorrenciaTipo = request.RecorrenciaTipo;
        servico.ModifiedDate = DateTime.UtcNow;

        _db.SaveChanges();

        return servico;
    }

    public void Deletar(int id, int idEmpresa)
    {
        var servico = _db.Servicos.FirstOrDefault(s => s.Id == id && s.IdEmpresa == idEmpresa)
            ?? throw new AppException("Modalidade não encontrada.");

        _db.Servicos.Remove(servico);
        _db.SaveChanges();
    }
}
