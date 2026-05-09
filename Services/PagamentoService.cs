public class PagamentoService
{
    private readonly AppDbContext _db;

    public PagamentoService(AppDbContext db)
    {
        _db = db;
    }

    public object ConfirmarPagamento(ConfirmarPagamentoRequest request)
    {
        var cliente = _db.Clientes.FirstOrDefault(c => c.Id == request.ClienteId);

        if (cliente is null)
            throw new Exception("Cliente não encontrado.");

        var vencimentoAnterior = cliente.DataVencimento;

        cliente.DataVencimento = request.NovoVencimento;
        cliente.DataUltimoPagamento = DateTime.UtcNow;
        cliente.Selos = Array.Empty<int>();

        var historico = new HistoricoPagamento
        {
            ClienteId = cliente.Id.ToString(),
            IdEmpresa = cliente.IdEmpresa,
            Valor = request.Valor,
            DataPagamento = DateTime.UtcNow,
            DataVencimentoAnterior = vencimentoAnterior,
            CreatedDate = DateTime.UtcNow
        };

        _db.HistoricoPagamentos.Add(historico);

        _db.SaveChanges();

        return new
        {
            mensagem = "Pagamento confirmado com sucesso",
            cliente,
            historico
        };
    }
}