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

    /// <summary>
    /// Garante que a empresa tenha um nome de sessão WAHA — empresas registradas antes desse campo existir
    /// não têm um ainda, então gera e persiste sob demanda em vez de exigir configuração manual.
    /// O nome da sessão é a única barreira entre tenants no WAHA (a API key é global), então ele só é
    /// gravado pelo servidor — nunca aceitar esse valor vindo do cliente.
    /// </summary>
    public string GarantirWahaSession(int idEmpresa)
    {
        var empresa = Obter(idEmpresa);

        if (!string.IsNullOrWhiteSpace(empresa.WahaSession))
            return empresa.WahaSession;

        empresa.WahaSession = $"empresa_{idEmpresa}";
        empresa.ModifiedDate = DateTime.UtcNow;
        _db.SaveChanges();

        return empresa.WahaSession;
    }
}
