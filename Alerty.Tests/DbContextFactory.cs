using Microsoft.EntityFrameworkCore;
 
public static class DbContextFactory
{
    /// <summary>
    /// Cria um AppDbContext usando banco InMemory com nome único por chamada.
    /// Isso garante que cada teste roda com banco limpo e isolado.
    /// </summary>
    public static AppDbContext Criar(string? nomeBanco = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nomeBanco ?? Guid.NewGuid().ToString())
            .Options;
 
        return new AppDbContext(options);
    }
}