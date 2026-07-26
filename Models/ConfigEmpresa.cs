public class ConfigEmpresa
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public string? NomeDono { get; set; }
    public string? WhatsappDono { get; set; }
    public int? LimiteNotificacoes { get; set; }
    public Plano Plano { get; set; } = Plano.Basico;
    public string? WahaSession { get; set; }
    public int? IdEmpresa { get; set; }
    public int[]? Selos { get; set; }
    public string? LinkLogo { get; set; }
    public DateTime? DataVencimento { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
