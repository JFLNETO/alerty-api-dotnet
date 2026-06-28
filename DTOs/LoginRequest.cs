public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Senha { get; set; } = null!;
    public bool ManterConectado { get; set; }
}
