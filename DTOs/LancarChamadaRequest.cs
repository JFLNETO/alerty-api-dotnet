public class LancarChamadaRequest
{
    public DateOnly? Data { get; set; }
    public List<ItemLancamentoChamada>? Itens { get; set; }
}

public class ItemLancamentoChamada
{
    public int IdCliente { get; set; }
    public StatusPresenca Status { get; set; }
}
