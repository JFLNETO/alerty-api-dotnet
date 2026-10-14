public class GraduacaoStatusResponse
{
    public int IdCliente { get; set; }
    public string? NomeCliente { get; set; }
    public int IdServico { get; set; }
    public string? NomeServico { get; set; }
    public int IdGraduacaoAtual { get; set; }
    public string? NomeGraduacaoAtual { get; set; }
    public DateOnly DataUltimaGraduacao { get; set; }
    public int? IdProximaGraduacao { get; set; }
    public string? NomeProximaGraduacao { get; set; }
    public int DiasNaGraduacaoAtual { get; set; }
    public int PresencasDesdeUltimaGraduacao { get; set; }
    public bool Apto { get; set; }

    /// <summary>0 a 100 — o menor entre progresso de tempo e de frequência (o critério que ainda falta).</summary>
    public int ProgressoPercentual { get; set; }
}
