public class Turma
{
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdServico { get; set; }
    public string? Nome { get; set; }
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
