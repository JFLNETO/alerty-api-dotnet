public class RegraAlertaService
{
    private readonly AppDbContext _db;

    public RegraAlertaService(AppDbContext db)
    {
        _db = db;
    }

    public List<RegraAlerta> Listar(int idEmpresa)
    {
        return _db.RegrasAlerta
            .Where(r => r.IdEmpresa == idEmpresa)
            .OrderBy(r => r.Tipo)
            .ThenBy(r => r.DiasOffset)
            .ToList();
    }

    public RegraAlerta Criar(CriarRegraAlertaRequest request, int idEmpresa)
    {
        if (string.IsNullOrWhiteSpace(request.Mensagem))
            throw new AppException("Mensagem é obrigatória.");

        var diasOffset = request.Tipo == TipoAlerta.NoDia ? 0 : request.DiasOffset;

        if (request.Tipo != TipoAlerta.NoDia && diasOffset <= 0)
            throw new AppException("Informe uma quantidade de dias maior que zero.");

        GarantirLimiteDoPlano(idEmpresa, alertasAdicionais: 1);

        var regra = new RegraAlerta
        {
            IdEmpresa = idEmpresa,
            Tipo = request.Tipo,
            DiasOffset = diasOffset,
            Mensagem = request.Mensagem,
            Ativo = true,
            CreatedDate = DateTime.UtcNow
        };

        _db.RegrasAlerta.Add(regra);
        _db.SaveChanges();

        return regra;
    }

    public RegraAlerta Atualizar(int id, AtualizarRegraAlertaRequest request, int idEmpresa)
    {
        var regra = _db.RegrasAlerta.FirstOrDefault(r => r.Id == id && r.IdEmpresa == idEmpresa)
            ?? throw new AppException("Regra de alerta não encontrada.");

        if (string.IsNullOrWhiteSpace(request.Mensagem))
            throw new AppException("Mensagem é obrigatória.");

        var diasOffset = request.Tipo == TipoAlerta.NoDia ? 0 : request.DiasOffset;

        if (request.Tipo != TipoAlerta.NoDia && diasOffset <= 0)
            throw new AppException("Informe uma quantidade de dias maior que zero.");

        regra.Tipo = request.Tipo;
        regra.DiasOffset = diasOffset;
        regra.Mensagem = request.Mensagem;

        _db.SaveChanges();

        return regra;
    }

    public RegraAlerta AlterarStatus(int id, bool ativo, int idEmpresa)
    {
        var regra = _db.RegrasAlerta.FirstOrDefault(r => r.Id == id && r.IdEmpresa == idEmpresa)
            ?? throw new AppException("Regra de alerta não encontrada.");

        if (ativo && !regra.Ativo)
            GarantirLimiteDoPlano(idEmpresa, alertasAdicionais: 1);

        regra.Ativo = ativo;
        _db.SaveChanges();

        return regra;
    }

    public void Deletar(int id, int idEmpresa)
    {
        var regra = _db.RegrasAlerta.FirstOrDefault(r => r.Id == id && r.IdEmpresa == idEmpresa)
            ?? throw new AppException("Regra de alerta não encontrada.");

        _db.RegrasAlerta.Remove(regra);
        _db.SaveChanges();
    }

    private void GarantirLimiteDoPlano(int idEmpresa, int alertasAdicionais)
    {
        var empresa = _db.ConfigEmpresas.FirstOrDefault(e => e.IdEmpresa == idEmpresa)
            ?? throw new AppException("Empresa não encontrada.");

        var alertasAtivos = _db.RegrasAlerta.Count(r => r.IdEmpresa == idEmpresa && r.Ativo);
        var limite = PlanoLimites.MaxAlertas(empresa.Plano);

        if (alertasAtivos + alertasAdicionais > limite)
            throw new AppException($"O plano atual permite no máximo {limite} alerta(s) ativo(s). Desative outro alerta antes de criar/ativar este.");
    }
}
