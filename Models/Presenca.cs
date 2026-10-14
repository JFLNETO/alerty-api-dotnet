public class Presenca
{
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdCliente { get; set; }
    public int IdTurma { get; set; }
    public DateOnly Data { get; set; }
    public StatusPresenca Status { get; set; }
    public int LancadoPor { get; set; }
    public DateTime DataHoraLancamento { get; set; }
}
