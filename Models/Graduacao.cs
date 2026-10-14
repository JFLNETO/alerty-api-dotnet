public class Graduacao
{
    public int Id { get; set; }
    public int IdEmpresa { get; set; }
    public int IdServico { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public int RequisitoDias { get; set; }
    public int RequisitoPresencas { get; set; }
    public DateTime CreatedDate { get; set; }
}
