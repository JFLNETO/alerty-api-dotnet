using System.Text;

public static class JwtConfig
{
    /// <summary>HS256 exige chave de pelo menos 256 bits (RFC 7518 §3.2).</summary>
    public const int TamanhoMinimoSecretBytes = 32;

    /// <summary>
    /// Falha no startup se o segredo JWT estiver ausente ou curto demais — melhor a API não subir do que
    /// subir assinando tokens com uma chave fraca (quem descobre o segredo forja id_empresa e is_admin).
    /// Tamanho não mede aleatoriedade: em produção o valor deve vir de `openssl rand -base64 64`.
    /// </summary>
    public static string ObterSecretValidado(IConfiguration config)
    {
        var secret = config["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Jwt:Secret não configurado.");

        if (Encoding.UTF8.GetByteCount(secret) < TamanhoMinimoSecretBytes)
            throw new InvalidOperationException(
                $"Jwt:Secret precisa ter pelo menos {TamanhoMinimoSecretBytes} bytes. Gere um com `openssl rand -base64 64`.");

        return secret;
    }
}
