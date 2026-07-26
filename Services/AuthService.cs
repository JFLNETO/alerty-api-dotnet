using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public object Registrar(RegistrarRequest request)
    {
        // -------------------------------------------------------
        // VALIDAÇÕES
        // -------------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new AppException("E-mail é obrigatório.");

        if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new AppException("E-mail inválido.");

        if (string.IsNullOrWhiteSpace(request.Senha))
            throw new AppException("Senha é obrigatória.");

        if (request.Senha.Length < 6)
            throw new AppException("A senha deve ter no mínimo 6 caracteres.");

        if (string.IsNullOrWhiteSpace(request.NomeDono))
            throw new AppException("Nome do dono é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.NomeEmpresa))
            throw new AppException("Nome da empresa é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.WhatsappDono))
            throw new AppException("WhatsApp é obrigatório.");

        // Remove tudo que não for número para validar o WhatsApp
        var whatsappNumeros = Regex.Replace(request.WhatsappDono, @"\D", "");

        if (whatsappNumeros.Length < 10 || whatsappNumeros.Length > 13)
            throw new AppException("WhatsApp inválido. Informe com DDD e número.");

        if (_db.Usuarios.Any(u => u.Email == request.Email))
            throw new AppException("E-mail já cadastrado.");

        // -------------------------------------------------------
        // CRIA A EMPRESA EM config_empresa
        // O id_unico é SERIAL — gerado automaticamente pelo banco.
        // O id_empresa é preenchido igual ao id_unico após o insert.
        // -------------------------------------------------------

        var empresa = new ConfigEmpresa
        {
            Nome = request.NomeEmpresa,
            NomeDono = request.NomeDono,
            WhatsappDono = whatsappNumeros,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            LimiteNotificacoes = 100,
            Selos = Array.Empty<int>()
        };

        _db.ConfigEmpresas.Add(empresa);
        _db.SaveChanges();

        // Após o SaveChanges, o EF Core popula empresa.Id com o SERIAL gerado.
        // Usamos esse Id como IdEmpresa (coluna id_empresa da mesma tabela).
        empresa.IdEmpresa = empresa.Id;

        // Nome da sessão do WAHA gerado automaticamente — o dono não precisa inventar um nome,
        // só escanear o QR code depois na tela de conexão do WhatsApp.
        empresa.WahaSession = $"empresa_{empresa.Id}";

        _db.SaveChanges();

        // -------------------------------------------------------
        // CRIA O USUÁRIO vinculado à empresa recém-criada
        // -------------------------------------------------------

        var usuario = new Usuario
        {
            Email = request.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            IdEmpresa = empresa.Id,
            CreatedDate = DateTime.UtcNow
        };

        _db.Usuarios.Add(usuario);
        _db.SaveChanges();

        return new
        {
            mensagem = "Cadastro realizado com sucesso.",
            idUsuario = usuario.Id,
            idEmpresa = empresa.Id,
            nomeEmpresa = empresa.Nome
        };
    }

    public object Login(string email, string senha, bool manterConectado)
    {
        var usuario = _db.Usuarios.FirstOrDefault(u => u.Email == email)
            ?? throw new AppException("Credenciais inválidas.");

        if (!BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash))
            throw new AppException("Credenciais inválidas.");

        var accessToken = GerarAccessToken(usuario);
        var refreshToken = GerarRefreshTokenString();

        // manterConectado = true  → 30 dias
        // manterConectado = false → 1 dia (sessão do dia)
        var expiracao = manterConectado
            ? DateTime.UtcNow.AddDays(30)
            : DateTime.UtcNow.AddDays(1);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            IdUsuario = usuario.Id,
            ExpiraEm = expiracao,
            CriadoEm = DateTime.UtcNow
        });

        _db.SaveChanges();

        return new
        {
            accessToken,
            refreshToken,
            expiresIn = 8 * 3600, // 8 horas em segundos (útil para o frontend)
            idEmpresa = usuario.IdEmpresa
        };
    }

    public object Refresh(string refreshToken)
    {
        var token = _db.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken);

        if (token is null || token.ExpiraEm < DateTime.UtcNow)
            throw new AppException("Refresh token inválido ou expirado.");

        var usuario = _db.Usuarios.FirstOrDefault(u => u.Id == token.IdUsuario)
            ?? throw new AppException("Usuário não encontrado.");

        return new { accessToken = GerarAccessToken(usuario) };
    }

    public void Logout(string refreshToken)
    {
        var token = _db.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken);

        if (token is not null)
        {
            _db.RefreshTokens.Remove(token);
            _db.SaveChanges();
        }
    }

    private string GerarAccessToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("id_empresa", usuario.IdEmpresa.ToString())
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private static string GerarRefreshTokenString()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
