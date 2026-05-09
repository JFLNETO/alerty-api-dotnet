public class ConfirmarPagamentoRequest
{
    public int ClienteId { get; set; }
    public decimal Valor { get; set; }
    public DateOnly NovoVencimento { get; set; }
}