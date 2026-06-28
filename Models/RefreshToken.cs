public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public int IdUsuario { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime CriadoEm { get; set; }
}
