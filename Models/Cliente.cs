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
    public int[]? IdTurmas { get; set; }
    public int[]? Selos { get; set; }
    public int IdEmpresa { get; set; }
    public string? UrlFoto { get; set; }

    /// <summary>
    /// Telefone sobre o qual o dono já foi avisado que não dá pra mandar WhatsApp — evita repetir o aviso
    /// todo dia. Se o dono trocar o telefone e o novo também falhar, os valores deixam de bater e ele é
    /// avisado de novo.
    /// </summary>
    public string? TelefoneAvisoInvalido { get; set; }
}