public class AlertaJobServiceTests
{
    [Fact]
    public void MontarAvisoContatosInvalidos_DeveIncluirLinkDoClientePorAluno_QuandoFrontendConfigurado()
    {
        // Arrange
        var contatos = new[]
        {
            new AlertaJobService.ContatoInvalido(12, "João", MotivoFalhaEnvio.NumeroSemWhatsApp),
            new AlertaJobService.ContatoInvalido(30, "Maria", MotivoFalhaEnvio.TelefoneInvalido)
        };

        // Act
        var aviso = AlertaJobService.MontarAvisoContatosInvalidos(contatos, "https://www.checkygo.com.br/");

        // Assert
        Assert.Contains("2 aluno(s)", aviso);
        Assert.Contains("- João: número sem WhatsApp\n  https://www.checkygo.com.br/?cliente=12", aviso);
        Assert.Contains("- Maria: telefone inválido\n  https://www.checkygo.com.br/?cliente=30", aviso);
    }

    [Fact]
    public void MontarAvisoContatosInvalidos_DeveListarSoOsNomes_QuandoFrontendNaoConfigurado()
    {
        // Arrange
        var contatos = new[] { new AlertaJobService.ContatoInvalido(12, "João", MotivoFalhaEnvio.NumeroSemWhatsApp) };

        // Act
        var aviso = AlertaJobService.MontarAvisoContatosInvalidos(contatos, "");

        // Assert
        Assert.Contains("- João: número sem WhatsApp", aviso);
        Assert.DoesNotContain("?cliente=", aviso);
    }

    [Fact]
    public void MontarAvisoContatosInvalidos_DeveListarAlunoUmaVez_QuandoDuasRegrasFalhamProMesmoAluno()
    {
        // Arrange
        var contatos = new[]
        {
            new AlertaJobService.ContatoInvalido(12, "João", MotivoFalhaEnvio.NumeroSemWhatsApp),
            new AlertaJobService.ContatoInvalido(12, "João", MotivoFalhaEnvio.NumeroSemWhatsApp)
        };

        // Act
        var aviso = AlertaJobService.MontarAvisoContatosInvalidos(contatos, "https://www.checkygo.com.br");

        // Assert
        Assert.Contains("1 aluno(s)", aviso);
        Assert.Equal(2, aviso.Split("?cliente=12").Length);
    }
}
