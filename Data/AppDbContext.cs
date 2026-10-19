using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<HistoricoPagamento> HistoricoPagamentos { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ConfigEmpresa> ConfigEmpresas { get; set; }
    public DbSet<Servico> Servicos { get; set; }
    public DbSet<RegraAlerta> RegrasAlerta { get; set; }
    public DbSet<HistoricoNotificacao> HistoricoNotificacoes { get; set; }
    public DbSet<Turma> Turmas { get; set; }
    public DbSet<Presenca> Presencas { get; set; }
    public DbSet<Graduacao> Graduacoes { get; set; }
    public DbSet<ClienteGraduacao> ClienteGraduacoes { get; set; }
    public DbSet<HistoricoGraduacao> HistoricoGraduacoes { get; set; }

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
            entity.Property(c => c.IdTurmas).HasColumnName("id_turmas");
            entity.Property(c => c.Selos).HasColumnName("selos");
            entity.Property(c => c.TelefoneAvisoInvalido).HasColumnName("telefone_aviso_invalido");
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
            entity.Property(u => u.IsAdmin).HasColumnName("is_admin");
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
            entity.Property(e => e.Plano).HasColumnName("plano");
            entity.Property(e => e.WahaSession).HasColumnName("waha_session");
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

        modelBuilder.Entity<RegraAlerta>(entity =>
        {
            entity.ToTable("regras_alerta", "alerty");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(r => r.Tipo).HasColumnName("tipo");
            entity.Property(r => r.DiasOffset).HasColumnName("dias_offset");
            entity.Property(r => r.Mensagem).HasColumnName("mensagem");
            entity.Property(r => r.Ativo).HasColumnName("ativo");
            entity.Property(r => r.CreatedDate).HasColumnName("created_date");
        });

        modelBuilder.Entity<HistoricoNotificacao>(entity =>
        {
            entity.ToTable("historico_notificacoes", "alerty");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id");
            entity.Property(h => h.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(h => h.IdCliente).HasColumnName("id_cliente");
            entity.Property(h => h.IdRegraAlerta).HasColumnName("id_regra_alerta");
            entity.Property(h => h.DataVencimentoReferencia).HasColumnName("data_vencimento_referencia");
            entity.Property(h => h.DataEnvio).HasColumnName("data_envio");
            entity.Property(h => h.Sucesso).HasColumnName("sucesso");
            entity.Property(h => h.ErroMensagem).HasColumnName("erro_mensagem");
        });

        modelBuilder.Entity<Turma>(entity =>
        {
            entity.ToTable("turmas", "alerty");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id_unico");
            entity.Property(t => t.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(t => t.IdServico).HasColumnName("id_servico");
            entity.Property(t => t.Nome).HasColumnName("nome");
            entity.Property(t => t.DiaSemana).HasColumnName("dia_semana");
            entity.Property(t => t.HoraInicio).HasColumnName("hora_inicio");
            entity.Property(t => t.HoraFim).HasColumnName("hora_fim");
            entity.Property(t => t.Ativo).HasColumnName("ativo");
            entity.Property(t => t.CreatedDate).HasColumnName("created_date");
            entity.Property(t => t.ModifiedDate).HasColumnName("modified_date");
        });

        modelBuilder.Entity<Presenca>(entity =>
        {
            entity.ToTable("presencas", "alerty");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("id_unico");
            entity.Property(p => p.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(p => p.IdCliente).HasColumnName("id_cliente");
            entity.Property(p => p.IdTurma).HasColumnName("id_turma");
            entity.Property(p => p.Data).HasColumnName("data");
            entity.Property(p => p.Status).HasColumnName("status");
            entity.Property(p => p.LancadoPor).HasColumnName("lancado_por");
            entity.Property(p => p.DataHoraLancamento).HasColumnName("data_hora_lancamento");

            entity.HasIndex(p => new { p.IdTurma, p.Data, p.IdCliente }).IsUnique();
        });

        modelBuilder.Entity<Graduacao>(entity =>
        {
            entity.ToTable("graduacoes", "alerty");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).HasColumnName("id_unico");
            entity.Property(g => g.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(g => g.IdServico).HasColumnName("id_servico");
            entity.Property(g => g.Nome).HasColumnName("nome");
            entity.Property(g => g.Ordem).HasColumnName("ordem");
            entity.Property(g => g.RequisitoDias).HasColumnName("requisito_dias");
            entity.Property(g => g.RequisitoPresencas).HasColumnName("requisito_presencas");
            entity.Property(g => g.CreatedDate).HasColumnName("created_date");
        });

        modelBuilder.Entity<ClienteGraduacao>(entity =>
        {
            entity.ToTable("cliente_graduacoes", "alerty");
            entity.HasKey(cg => cg.Id);
            entity.Property(cg => cg.Id).HasColumnName("id_unico");
            entity.Property(cg => cg.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(cg => cg.IdCliente).HasColumnName("id_cliente");
            entity.Property(cg => cg.IdServico).HasColumnName("id_servico");
            entity.Property(cg => cg.IdGraduacaoAtual).HasColumnName("id_graduacao_atual");
            entity.Property(cg => cg.DataUltimaGraduacao).HasColumnName("data_ultima_graduacao");

            entity.HasIndex(cg => new { cg.IdCliente, cg.IdServico }).IsUnique();
        });

        modelBuilder.Entity<HistoricoGraduacao>(entity =>
        {
            entity.ToTable("historico_graduacoes", "alerty");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id_unico");
            entity.Property(h => h.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(h => h.IdCliente).HasColumnName("id_cliente");
            entity.Property(h => h.IdServico).HasColumnName("id_servico");
            entity.Property(h => h.IdGraduacaoAnterior).HasColumnName("id_graduacao_anterior");
            entity.Property(h => h.IdGraduacaoNova).HasColumnName("id_graduacao_nova");
            entity.Property(h => h.Data).HasColumnName("data");
            entity.Property(h => h.ConfirmadoPor).HasColumnName("confirmado_por");
        });
    }
}
