/// <summary>
/// Por que um envio pelo WhatsApp falhou. Separa o que é problema do cadastro do aluno (o dono da
/// academia consegue corrigir) do que é problema de infraestrutura (só o suporte resolve).
/// </summary>
public enum MotivoFalhaEnvio
{
    Nenhum = 0,
    TelefoneInvalido = 1,
    NumeroSemWhatsApp = 2,
    Outro = 3
}
