public class ClienteGraduacao
{
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdCliente { get; set; }
    public int IdServico { get; set; }
    public int IdGraduacaoAtual { get; set; }
    public DateOnly DataUltimaGraduacao { get; set; }
}
