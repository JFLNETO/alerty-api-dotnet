public class Cliente
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public string? IdCliente { get; set; }
    public string? Telefone { get; set; }
    public bool Ativo { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateTime? DataUltimoPagamento { get; set; }
    public int[]? IdServicos { get; set; }
    public int[]? Selos { get; set; }
    public int IdEmpresa { get; set; }
    public string? UrlFoto { get; set; }
}