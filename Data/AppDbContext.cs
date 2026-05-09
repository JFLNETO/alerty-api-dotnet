using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<HistoricoPagamento> HistoricoPagamentos { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===============================
        // CLIENTES
        // ===============================
        _ = modelBuilder.Entity<Cliente>(static entity =>
        {
            entity.ToTable("clientes", "alerty");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                  .HasColumnName("id_unico");

            entity.Property(c => c.Nome)
                  .HasColumnName("nome");

            entity.Property(c => c.Telefone)
                  .HasColumnName("telefone");

            entity.Property(c => c.Ativo)
                  .HasColumnName("ativo");

            entity.Property(c => c.DataVencimento)
                  .HasColumnName("data_vencimento");

            entity.Property(c => c.DataUltimoPagamento)
                  .HasColumnName("data_ultimo_pagamento");

            entity.Property(c => c.UrlFoto)
                  .HasColumnName("url_foto");

            entity.Property(c => c.IdEmpresa)
                  .HasColumnName("id_empresa");

            entity.Property(c => c.IdCliente)
                  .HasColumnName("id_cliente");

            entity.Property(c => c.IdServicos)
                  .HasColumnName("id_servicos");

            entity.Property(c => c.Selos)
                  .HasColumnName("selos");
        });

        // ===============================
        // HISTÓRICO DE PAGAMENTOS
        // ===============================
        modelBuilder.Entity<HistoricoPagamento>(entity =>
        {
            entity.ToTable("historico_cobranca", "alerty");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                  .HasColumnName("id_unico");

            entity.Property(p => p.ClienteId)
                  .HasColumnName("id_cliente");

            entity.Property(p => p.IdEmpresa)
                  .HasColumnName("id_empresa");

            entity.Property(p => p.Valor)
                  .HasColumnName("valor");

            entity.Property(p => p.DataPagamento)
                  .HasColumnName("data_pagamento");

            entity.Property(p => p.DataVencimentoAnterior)
                  .HasColumnName("data_vencimento");

            entity.Property(p => p.CreatedDate)
                  .HasColumnName("created_date");
        });
    }
}