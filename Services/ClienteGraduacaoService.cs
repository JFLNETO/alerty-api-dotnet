public class ClienteGraduacaoService
{
    private readonly AppDbContext _db;

    public ClienteGraduacaoService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Cria o estado inicial de graduação (primeira graduação cadastrada da modalidade) para cada
    /// serviço novo em que o cliente foi matriculado. Serviços sem nenhuma graduação cadastrada ainda
    /// são ignorados — o estado é criado depois, quando o dono cadastrar as faixas.
    /// </summary>
    public void SincronizarMatriculas(int idCliente, int idEmpresa, int[]? idServicosAtuais)
    {
        if (idServicosAtuais is null || idServicosAtuais.Length == 0)
            return;

        var idsExistentes = _db.ClienteGraduacoes
            .Where(cg => cg.IdCliente == idCliente && cg.IdEmpresa == idEmpresa)
            .Select(cg => cg.IdServico)
            .ToHashSet();

        var servicosNovos = idServicosAtuais.Where(id => !idsExistentes.Contains(id)).ToList();
        if (servicosNovos.Count == 0)
            return;

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var idServico in servicosNovos)
        {
            var primeiraGraduacao = _db.Graduacoes
                .Where(g => g.IdServico == idServico && g.IdEmpresa == idEmpresa)
                .OrderBy(g => g.Ordem)
                .FirstOrDefault();

            if (primeiraGraduacao is null)
                continue;

            _db.ClienteGraduacoes.Add(new ClienteGraduacao
            {
                IdEmpresa = idEmpresa,
                IdCliente = idCliente,
                IdServico = idServico,
                IdGraduacaoAtual = primeiraGraduacao.Id,
                DataUltimaGraduacao = hoje
            });
        }

        _db.SaveChanges();
    }

    public List<GraduacaoStatusResponse> ListarPorCliente(int idCliente, int idEmpresa)
    {
        _ = _db.Clientes.FirstOrDefault(c => c.Id == idCliente && c.IdEmpresa == idEmpresa)
            ?? throw new AppException("Cliente não encontrado.");

        return _db.ClienteGraduacoes
            .Where(cg => cg.IdCliente == idCliente && cg.IdEmpresa == idEmpresa)
            .ToList()
            .Select(MontarStatus)
            .ToList();
    }

    /// <summary>
    /// Define (ou corrige) a graduação atual do cliente numa modalidade — usado tanto pro caso do
    /// aluno que já chega graduado de outra academia quanto pra correção manual de erro de lançamento.
    /// </summary>
    public ClienteGraduacao DefinirGraduacaoAtual(
        int idCliente,
        int idServico,
        DefinirGraduacaoAtualRequest request,
        int idEmpresa)
    {
        var cliente = _db.Clientes.FirstOrDefault(c => c.Id == idCliente && c.IdEmpresa == idEmpresa)
            ?? throw new AppException("Cliente não encontrado.");

        if (cliente.IdServicos is null || !cliente.IdServicos.Contains(idServico))
            throw new AppException("Cliente não está matriculado nessa modalidade.");

        if (request.IdGraduacao is null)
            throw new AppException("Graduação é obrigatória.");

        var graduacao = _db.Graduacoes
            .FirstOrDefault(g => g.Id == request.IdGraduacao && g.IdServico == idServico && g.IdEmpresa == idEmpresa)
            ?? throw new AppException("Graduação não encontrada para essa modalidade.");

        var data = request.Data ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var estado = _db.ClienteGraduacoes
            .FirstOrDefault(cg => cg.IdCliente == idCliente && cg.IdServico == idServico && cg.IdEmpresa == idEmpresa);

        if (estado is null)
        {
            estado = new ClienteGraduacao
            {
                IdEmpresa = idEmpresa,
                IdCliente = idCliente,
                IdServico = idServico,
                IdGraduacaoAtual = graduacao.Id,
                DataUltimaGraduacao = data
            };
            _db.ClienteGraduacoes.Add(estado);
        }
        else
        {
            estado.IdGraduacaoAtual = graduacao.Id;
            estado.DataUltimaGraduacao = data;
        }

        _db.SaveChanges();

        return estado;
    }

    /// <summary>
    /// Promove o cliente para a próxima graduação da ordem cadastrada. Nunca bloqueia por
    /// frequência/tempo — a avaliação técnica do professor é sempre soberana, o sistema só informa.
    /// </summary>
    public GraduacaoStatusResponse Graduar(int idCliente, GraduarClienteRequest request, int idUsuario, int idEmpresa)
    {
        if (request.IdServico is null)
            throw new AppException("Modalidade é obrigatória.");

        _ = _db.Clientes.FirstOrDefault(c => c.Id == idCliente && c.IdEmpresa == idEmpresa)
            ?? throw new AppException("Cliente não encontrado.");

        var estado = _db.ClienteGraduacoes.FirstOrDefault(cg =>
                cg.IdCliente == idCliente && cg.IdServico == request.IdServico && cg.IdEmpresa == idEmpresa)
            ?? throw new AppException("Cliente ainda não tem graduação inicial definida nessa modalidade.");

        var atual = _db.Graduacoes.First(g => g.Id == estado.IdGraduacaoAtual);

        var proxima = _db.Graduacoes
            .Where(g => g.IdServico == request.IdServico && g.IdEmpresa == idEmpresa && g.Ordem > atual.Ordem)
            .OrderBy(g => g.Ordem)
            .FirstOrDefault()
            ?? throw new AppException("Cliente já está na última graduação cadastrada nessa modalidade.");

        var agora = DateTime.UtcNow;

        _db.HistoricoGraduacoes.Add(new HistoricoGraduacao
        {
            IdEmpresa = idEmpresa,
            IdCliente = idCliente,
            IdServico = request.IdServico.Value,
            IdGraduacaoAnterior = atual.Id,
            IdGraduacaoNova = proxima.Id,
            Data = agora,
            ConfirmadoPor = idUsuario
        });

        estado.IdGraduacaoAtual = proxima.Id;
        estado.DataUltimaGraduacao = DateOnly.FromDateTime(agora);

        _db.SaveChanges();

        return MontarStatus(estado);
    }

    /// <summary>
    /// Relatório de progressão de graduação: todo aluno ativo com estado de graduação numa modalidade
    /// que ainda tem uma próxima graduação pela frente (quem já está na última graduação cadastrada
    /// não aparece — não há progresso a mostrar). Ordenado por progresso decrescente, então os aptos
    /// (progresso 100%) sempre aparecem no topo.
    /// </summary>
    public List<GraduacaoStatusResponse> ListarProgressoGraduacao(int? idServico, int idEmpresa)
    {
        var query = _db.ClienteGraduacoes.Where(cg => cg.IdEmpresa == idEmpresa);

        if (idServico is not null)
            query = query.Where(cg => cg.IdServico == idServico);

        var idsClientesAtivos = _db.Clientes
            .Where(c => c.IdEmpresa == idEmpresa && c.Ativo)
            .Select(c => c.Id)
            .ToHashSet();

        return query
            .ToList()
            .Where(cg => idsClientesAtivos.Contains(cg.IdCliente))
            .Select(MontarStatus)
            .Where(status => status.IdProximaGraduacao is not null)
            .OrderByDescending(status => status.ProgressoPercentual)
            .ThenBy(status => status.NomeCliente)
            .ToList();
    }

    private GraduacaoStatusResponse MontarStatus(ClienteGraduacao estado)
    {
        var cliente = _db.Clientes.First(c => c.Id == estado.IdCliente);
        var servico = _db.Servicos.First(s => s.Id == estado.IdServico);
        var atual = _db.Graduacoes.First(g => g.Id == estado.IdGraduacaoAtual);

        var proxima = _db.Graduacoes
            .Where(g => g.IdServico == estado.IdServico && g.IdEmpresa == estado.IdEmpresa && g.Ordem > atual.Ordem)
            .OrderBy(g => g.Ordem)
            .FirstOrDefault();

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var diasNaGraduacaoAtual = hoje.DayNumber - estado.DataUltimaGraduacao.DayNumber;

        var idsTurmasDoServico = _db.Turmas
            .Where(t => t.IdServico == estado.IdServico && t.IdEmpresa == estado.IdEmpresa)
            .Select(t => t.Id)
            .ToHashSet();

        var presencas = _db.Presencas.Count(p =>
            p.IdCliente == estado.IdCliente
            && p.Status == StatusPresenca.Presente
            && p.Data >= estado.DataUltimaGraduacao
            && idsTurmasDoServico.Contains(p.IdTurma));

        var apto = proxima is not null
            && diasNaGraduacaoAtual >= proxima.RequisitoDias
            && presencas >= proxima.RequisitoPresencas;

        var progressoPercentual = CalcularProgresso(proxima, diasNaGraduacaoAtual, presencas);

        return new GraduacaoStatusResponse
        {
            IdCliente = estado.IdCliente,
            NomeCliente = cliente.Nome,
            IdServico = estado.IdServico,
            NomeServico = servico.Nome,
            IdGraduacaoAtual = atual.Id,
            NomeGraduacaoAtual = atual.Nome,
            DataUltimaGraduacao = estado.DataUltimaGraduacao,
            IdProximaGraduacao = proxima?.Id,
            NomeProximaGraduacao = proxima?.Nome,
            DiasNaGraduacaoAtual = diasNaGraduacaoAtual,
            PresencasDesdeUltimaGraduacao = presencas,
            Apto = apto,
            ProgressoPercentual = progressoPercentual
        };
    }

    /// <summary>
    /// Progresso é o menor entre tempo e frequência — o critério que ainda falta pra ficar apto.
    /// Sem próxima graduação não há o que progredir. Requisito zerado conta como já cumprido (100%).
    /// </summary>
    private static int CalcularProgresso(Graduacao? proxima, int diasNaGraduacaoAtual, int presencas)
    {
        if (proxima is null)
            return 0;

        var progressoDias = proxima.RequisitoDias <= 0
            ? 1.0
            : Math.Min(1.0, (double)diasNaGraduacaoAtual / proxima.RequisitoDias);

        var progressoPresencas = proxima.RequisitoPresencas <= 0
            ? 1.0
            : Math.Min(1.0, (double)presencas / proxima.RequisitoPresencas);

        var progresso = Math.Min(progressoDias, progressoPresencas);

        return (int)Math.Round(progresso * 100);
    }
}
