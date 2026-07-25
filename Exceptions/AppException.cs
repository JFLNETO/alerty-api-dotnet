/// <summary>
/// Erro de validação/regra de negócio esperado — vira 400 com a mensagem exposta ao cliente.
/// Qualquer outra exceção é tratada como erro interno (500, mensagem genérica) pelo middleware global.
/// </summary>
public class AppException : Exception
{
    public AppException(string message) : base(message) { }
}
