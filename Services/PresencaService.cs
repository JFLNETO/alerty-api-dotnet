public class PresencaService
{
    private readonly AppDbContext _db;

    public PresencaService(AppDbContext db)
    {
        _db = db;
    }

    public List<AlunoChamadaResponse> BuscarChamada(int idTurma, DateOnly data, int idEmpresa)
    {
        var turma = _db.Turmas.FirstOrDefault(t => t.Id == idTurma && t.IdEmpresa == idEmpresa)
            ?? throw new AppException("Turma não encontrada.");

        var alunos = _db.Clientes
            .Where(c => c.IdEmpresa == idEmpresa
                && c.Ativo
                && c.IdServicos != null
                && c.IdServicos.Contains(turma.IdServico))
            .ToList();

        var presencasLancadas = _db.Presencas
            .Where(p => p.IdTurma == idTurma && p.Data == data)
            .ToList()
            .ToDictionary(p => p.IdCliente, p => p.Status);

        return alunos
            .Select(c => new AlunoChamadaResponse
            {
                IdCliente = c.Id,
                Nome = c.Nome,
                MatriculadoNestaTurma = c.IdTurmas != null && c.IdTurmas.Contains(idTurma),
                Status = presencasLancadas.TryGetValue(c.Id, out var status) ? status : null
            })
            .OrderByDescending(a => a.MatriculadoNestaTurma)
            .ThenBy(a => a.Nome)
            .ToList();
    }

    public List<Presenca> LancarChamada(int idTurma, LancarChamadaRequest request, int idUsuario, int idEmpresa)
    {
        var turma = _db.Turmas.FirstOrDefault(t => t.Id == idTurma && t.IdEmpresa == idEmpresa)
            ?? throw new AppException("Turma não encontrada.");

        if (request.Data is null)
            throw new AppException("Data é obrigatória.");

        if (request.Itens is null || request.Itens.Count == 0)
            throw new AppException("Informe ao menos um aluno na chamada.");

        var data = request.Data.Value;
        var agora = DateTime.UtcNow;

        var existentes = _db.Presencas
            .Where(p => p.IdTurma == idTurma && p.Data == data)
            .ToList()
            .ToDictionary(p => p.IdCliente);

        var resultado = new List<Presenca>();

        foreach (var item in request.Itens)
        {
            var clienteExiste = _db.Clientes
                .Any(c => c.Id == item.IdCliente && c.IdEmpresa == idEmpresa);

            if (!clienteExiste)
                throw new AppException($"Cliente {item.IdCliente} não encontrado.");

            if (existentes.TryGetValue(item.IdCliente, out var presenca))
            {
                presenca.Status = item.Status;
                presenca.LancadoPor = idUsuario;
                presenca.DataHoraLancamento = agora;
            }
            else
            {
                presenca = new Presenca
                {
                    IdEmpresa = idEmpresa,
                    IdCliente = item.IdCliente,
                    IdTurma = idTurma,
                    Data = data,
                    Status = item.Status,
                    LancadoPor = idUsuario,
                    DataHoraLancamento = agora
                };
                _db.Presencas.Add(presenca);
            }

            resultado.Add(presenca);
        }

        _db.SaveChanges();

        return resultado;
    }
}
