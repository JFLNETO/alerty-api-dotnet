public class HistoricoGraduacao
{
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdCliente { get; set; }
    public int IdServico { get; set; }
    public int IdGraduacaoAnterior { get; set; }
    public int IdGraduacaoNova { get; set; }
    public DateTime Data { get; set; }
    public int ConfirmadoPor { get; set; }
}
