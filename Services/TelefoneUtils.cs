public static class TelefoneUtils
{
    /// <summary>Remove tudo que não for dígito — é esse o formato salvo em Cliente.Telefone.</summary>
    public static string ApenasDigitos(string telefone) =>
        new(telefone.Where(char.IsDigit).ToArray());

    /// <summary>
    /// Normaliza um telefone (com ou sem +55, espaços, parênteses, traço) para o formato
    /// esperado nos JIDs do WhatsApp: só dígitos, com DDI 55 uma única vez.
    /// Números com 12 ou 13 dígitos já vêm com DDI; os demais recebem o prefixo 55.
    /// </summary>
    public static string NormalizarParaWhatsApp(string telefone)
    {
        var digitos = ApenasDigitos(telefone);
        return digitos.Length is 12 or 13 ? digitos : $"55{digitos}";
    }
}
