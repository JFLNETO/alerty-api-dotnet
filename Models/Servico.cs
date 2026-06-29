public class Servico
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public int IdEmpresa { get; set; }
    public decimal? Valor { get; set; }
    public int? RecorrenciaValor { get; set; }
    public string? RecorrenciaTipo { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
