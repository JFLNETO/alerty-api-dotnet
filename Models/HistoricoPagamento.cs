public class HistoricoPagamento
{
    public int Id { get; set; }
    public string? ClienteId { get; set; }
    public int IdEmpresa { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataPagamento { get; set; }
    public DateOnly DataVencimentoAnterior { get; set; }
    public DateTime CreatedDate { get; set; }
}