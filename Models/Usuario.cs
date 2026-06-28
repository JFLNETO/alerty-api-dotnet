public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string SenhaHash { get; set; } = null!;
    public int IdEmpresa { get; set; }
    public DateTime CreatedDate { get; set; }
}
