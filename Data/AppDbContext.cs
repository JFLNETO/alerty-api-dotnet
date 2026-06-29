using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<HistoricoPagamento> HistoricoPagamentos { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ConfigEmpresa> ConfigEmpresas { get; set; }
    public DbSet<Servico> Servicos { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes", "alerty");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id_unico");
            entity.Property(c => c.Nome).HasColumnName("nome");
            entity.Property(c => c.Telefone).HasColumnName("telefone");
            entity.Property(c => c.Ativo).HasColumnName("ativo");
            entity.Property(c => c.DataVencimento).HasColumnName("data_vencimento");
            entity.Property(c => c.DataUltimoPagamento).HasColumnName("data_ultimo_pagamento");
            entity.Property(c => c.UrlFoto).HasColumnName("url_foto");
            entity.Property(c => c.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(c => c.IdCliente).HasColumnName("id_cliente");
            entity.Property(c => c.IdServicos).HasColumnName("id_servicos");
            entity.Property(c => c.Selos).HasColumnName("selos");
        });

        modelBuilder.Entity<HistoricoPagamento>(entity =>
        {
            entity.ToTable("historico_cobranca", "alerty");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("id_unico");
            entity.Property(p => p.ClienteId).HasColumnName("id_cliente");
            entity.Property(p => p.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(p => p.Valor).HasColumnName("valor");
            entity.Property(p => p.DataPagamento).HasColumnName("data_pagamento");
            entity.Property(p => p.DataVencimentoAnterior).HasColumnName("data_vencimento");
            entity.Property(p => p.CreatedDate).HasColumnName("created_date");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios", "alerty");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.Email).HasColumnName("email");
            entity.Property(u => u.SenhaHash).HasColumnName("senha_hash");
            entity.Property(u => u.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(u => u.CreatedDate).HasColumnName("created_date");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens", "alerty");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.Token).HasColumnName("token");
            entity.Property(r => r.IdUsuario).HasColumnName("id_usuario");
            entity.Property(r => r.ExpiraEm).HasColumnName("expira_em");
            entity.Property(r => r.CriadoEm).HasColumnName("criado_em");
        });

        modelBuilder.Entity<ConfigEmpresa>(entity =>
        {
            entity.ToTable("config_empresa", "alerty");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_unico");
            entity.Property(e => e.Nome).HasColumnName("nome");
            entity.Property(e => e.NomeDono).HasColumnName("nome_dono");
            entity.Property(e => e.WhatsappDono).HasColumnName("whatsapp_dono");
            entity.Property(e => e.LimiteNotificacoes).HasColumnName("limite_notificacoes");
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.Selos).HasColumnName("selos");
            entity.Property(e => e.LinkLogo).HasColumnName("link_logo");
            entity.Property(e => e.DataVencimento).HasColumnName("data_vencimento");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
        });

        modelBuilder.Entity<Servico>(entity =>
        {
            entity.ToTable("servicos", "alerty");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).HasColumnName("id_unico");
            entity.Property(s => s.Nome).HasColumnName("nome");
            entity.Property(s => s.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(s => s.Valor).HasColumnName("valor");
            entity.Property(s => s.RecorrenciaValor).HasColumnName("recorrencia_valor");
            entity.Property(s => s.RecorrenciaTipo).HasColumnName("recorrencia_tipo");
            entity.Property(s => s.CreatedDate).HasColumnName("created_date");
            entity.Property(s => s.ModifiedDate).HasColumnName("modified_date");
        });
    }
}
