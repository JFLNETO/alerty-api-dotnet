public class GraduacaoService
{
    private static readonly string[] FaixasJiuJitsuAdulto = { "Branca", "Azul", "Roxa", "Marrom", "Preta" };

    private readonly AppDbContext _db;

    public GraduacaoService(AppDbContext db)
    {
        _db = db;
    }

    public List<Graduacao> Listar(int idServico, int idEmpresa)
    {
        return _db.Graduacoes
            .Where(g => g.IdServico == idServico && g.IdEmpresa == idEmpresa)
            .OrderBy(g => g.Ordem)
            .ToList();
    }

    public Graduacao Criar(CriarGraduacaoRequest request, int idEmpresa)
    {
        if (request.IdServico is null)
            throw new AppException("Modalidade é obrigatória.");

        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new AppException("Nome é obrigatório.");

        if (request.Ordem is null)
            throw new AppException("Ordem é obrigatória.");

        var servicoExiste = _db.Servicos.Any(s => s.Id == request.IdServico && s.IdEmpresa == idEmpresa);
        if (!servicoExiste)
            throw new AppException("Modalidade não encontrada.");

        var graduacao = new Graduacao
        {
            IdEmpresa = idEmpresa,
            IdServico = request.IdServico.Value,
            Nome = request.Nome,
            Ordem = request.Ordem.Value,
            RequisitoDias = request.RequisitoDias,
            RequisitoPresencas = request.RequisitoPresencas,
            CreatedDate = DateTime.UtcNow
        };

        _db.Graduacoes.Add(graduacao);
        _db.SaveChanges();

        return graduacao;
    }

    public Graduacao Atualizar(int id, AtualizarGraduacaoRequest request, int idEmpresa)
    {
        var graduacao = _db.Graduacoes.FirstOrDefault(g => g.Id == id && g.IdEmpresa == idEmpresa)
            ?? throw new AppException("Graduação não encontrada.");

        if (!string.IsNullOrWhiteSpace(request.Nome))
            graduacao.Nome = request.Nome;

        if (request.Ordem is not null)
            graduacao.Ordem = request.Ordem.Value;

        if (request.RequisitoDias is not null)
            graduacao.RequisitoDias = request.RequisitoDias.Value;

        if (request.RequisitoPresencas is not null)
            graduacao.RequisitoPresencas = request.RequisitoPresencas.Value;

        _db.SaveChanges();

        return graduacao;
    }

    public void Deletar(int id, int idEmpresa)
    {
        var graduacao = _db.Graduacoes.FirstOrDefault(g => g.Id == id && g.IdEmpresa == idEmpresa)
            ?? throw new AppException("Graduação não encontrada.");

        _db.Graduacoes.Remove(graduacao);
        _db.SaveChanges();
    }

    public List<Graduacao> CriarModeloPadraoJiuJitsu(int idServico, int idEmpresa)
    {
        var servicoExiste = _db.Servicos.Any(s => s.Id == idServico && s.IdEmpresa == idEmpresa);
        if (!servicoExiste)
            throw new AppException("Modalidade não encontrada.");

        var jaTemGraduacoes = _db.Graduacoes.Any(g => g.IdServico == idServico && g.IdEmpresa == idEmpresa);
        if (jaTemGraduacoes)
            throw new AppException("Essa modalidade já tem graduações cadastradas.");

        var criadas = new List<Graduacao>();

        for (var i = 0; i < FaixasJiuJitsuAdulto.Length; i++)
        {
            var graduacao = new Graduacao
            {
                IdEmpresa = idEmpresa,
                IdServico = idServico,
                Nome = FaixasJiuJitsuAdulto[i],
                Ordem = i + 1,
                RequisitoDias = 0,
                RequisitoPresencas = 0,
                CreatedDate = DateTime.UtcNow
            };
            _db.Graduacoes.Add(graduacao);
            criadas.Add(graduacao);
        }

        _db.SaveChanges();

        return criadas;
    }
}
