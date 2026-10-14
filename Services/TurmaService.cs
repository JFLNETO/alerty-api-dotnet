public class TurmaService
{
    private readonly AppDbContext _db;

    public TurmaService(AppDbContext db)
    {
        _db = db;
    }

    public List<Turma> Listar(int idEmpresa)
    {
        return _db.Turmas
            .Where(t => t.IdEmpresa == idEmpresa)
            .OrderBy(t => t.DiaSemana)
            .ThenBy(t => t.HoraInicio)
            .ToList();
    }

    public Turma Criar(CriarTurmaRequest request, int idEmpresa)
    {
        if (request.IdServico is null)
            throw new AppException("Modalidade é obrigatória.");

        if (request.DiaSemana is null)
            throw new AppException("Dia da semana é obrigatório.");

        if (request.HoraInicio is null || request.HoraFim is null)
            throw new AppException("Horário de início e fim são obrigatórios.");

        if (request.HoraFim <= request.HoraInicio)
            throw new AppException("Horário de fim deve ser depois do horário de início.");

        var servicoExiste = _db.Servicos
            .Any(s => s.Id == request.IdServico && s.IdEmpresa == idEmpresa);

        if (!servicoExiste)
            throw new AppException("Modalidade não encontrada.");

        var turma = new Turma
        {
            IdEmpresa = idEmpresa,
            IdServico = request.IdServico.Value,
            Nome = request.Nome,
            DiaSemana = request.DiaSemana.Value,
            HoraInicio = request.HoraInicio.Value,
            HoraFim = request.HoraFim.Value,
            Ativo = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _db.Turmas.Add(turma);
        _db.SaveChanges();

        return turma;
    }

    public Turma Atualizar(int id, AtualizarTurmaRequest request, int idEmpresa)
    {
        var turma = _db.Turmas.FirstOrDefault(t => t.Id == id && t.IdEmpresa == idEmpresa)
            ?? throw new AppException("Turma não encontrada.");

        if (!string.IsNullOrWhiteSpace(request.Nome))
            turma.Nome = request.Nome;

        if (request.DiaSemana is not null)
            turma.DiaSemana = request.DiaSemana.Value;

        if (request.HoraInicio is not null)
            turma.HoraInicio = request.HoraInicio.Value;

        if (request.HoraFim is not null)
            turma.HoraFim = request.HoraFim.Value;

        if (turma.HoraFim <= turma.HoraInicio)
            throw new AppException("Horário de fim deve ser depois do horário de início.");

        if (request.Ativo is not null)
            turma.Ativo = request.Ativo.Value;

        turma.ModifiedDate = DateTime.UtcNow;

        _db.SaveChanges();

        return turma;
    }

    public void Deletar(int id, int idEmpresa)
    {
        var turma = _db.Turmas.FirstOrDefault(t => t.Id == id && t.IdEmpresa == idEmpresa)
            ?? throw new AppException("Turma não encontrada.");

        _db.Turmas.Remove(turma);
        _db.SaveChanges();
    }
}
