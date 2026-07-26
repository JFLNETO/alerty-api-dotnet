public class RegraAlerta
{
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public TipoAlerta Tipo { get; set; }

    /// <summary>Dias antes/depois do vencimento. Ignorado quando Tipo = NoDia.</summary>
    public int DiasOffset { get; set; }

    public string Mensagem { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime CreatedDate { get; set; }
}
