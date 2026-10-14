public class CriarTurmaRequest
{
    public int? IdServico { get; set; }
    public string? Nome { get; set; }
    public DayOfWeek? DiaSemana { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
}
