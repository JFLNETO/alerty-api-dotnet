public class HistoricoNotificacao
{
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdCliente { get; set; }
    public int IdRegraAlerta { get; set; }

    /// <summary>Vencimento do cliente no momento do disparo — evita reenvio duplicado no mesmo ciclo.</summary>
    public DateOnly DataVencimentoReferencia { get; set; }

    public DateTime DataEnvio { get; set; }
    public bool Sucesso { get; set; }
    public string? ErroMensagem { get; set; }
}
