public class CriarClienteRequest
{
    public string? Nome { get; set; }

    public string? Telefone { get; set; }

    public DateOnly DataVencimento { get; set; }

    public int[]? IdServicos { get; set; }

    public int IdEmpresa { get; set; }

    public string? UrlFoto { get; set; }
}