public class ConfigEmpresaService
{
    private readonly AppDbContext _db;

    public ConfigEmpresaService(AppDbContext db)
    {
        _db = db;
    }

    public ConfigEmpresa Obter(int idEmpresa)
    {
        return _db.ConfigEmpresas.FirstOrDefault(e => e.IdEmpresa == idEmpresa)
            ?? throw new AppException("Empresa não encontrada.");
    }

    public ConfigEmpresa AtualizarWahaSessao(int idEmpresa, string? session)
    {
        if (string.IsNullOrWhiteSpace(session))
            throw new AppException("Informe o nome da sessão do WAHA.");

        var empresa = Obter(idEmpresa);
        empresa.WahaSession = session;
        empresa.ModifiedDate = DateTime.UtcNow;

        _db.SaveChanges();

        return empresa;
    }
}
