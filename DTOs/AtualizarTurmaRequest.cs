public class AtualizarTurmaRequest
{
    public string? Nome { get; set; }
    public DayOfWeek? DiaSemana { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
    public bool? Ativo { get; set; }
}
