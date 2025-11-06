/**
    * ApplicationDbContext
    *
    * Contexto do Entity Framework Core para acesso ao banco de dados do sistema PIM.
    * Contém DbSets para todas as entidades do sistema, permitindo CRUD e consultas.
    *
    * Funcionalidades:
    * - Representa tabelas como DbSet<T> para Users, Setores, Sessões, Chamados, Comentários, Histórico, Anexos, Cargos, Prioridades e Critérios de Prioridade.
    * - Configura entidades sem chave primária quando necessário (ex.: ChamadoComUsuario para resultados de JOINs).
    *
    * Dependências:
    * - DbContextOptions<ApplicationDbContext> para configuração do EF Core.
*/

using Microsoft.EntityFrameworkCore;
using PIM.Models;

public class ApplicationDbContext : DbContext
{
    /**
        * Construtor ApplicationDbContext
        *
        * Inicializa o contexto com opções fornecidas (como string de conexão, provedores, etc.).
        *
        * Tipo de retorno: N/A
        *
        * Parâmetros:
        * - DbContextOptions<ApplicationDbContext> options: opções de configuração do contexto EF Core.
    */
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    /**
        * DbSets
        *
        * Representam as tabelas do banco de dados:
        * - Usuarios: tabela de usuários do sistema.
        * - Setores: tabela de setores.
        * - Sessoes: tabela de sessões ativas de usuários.
        * - Chamados: tabela de chamados abertos/concluídos.
        * - ChamadosComUsuarios: resultado de consultas customizadas (JOIN entre Chamados e Usuários) sem chave primária.
        * - Comentarios: comentários relacionados a chamados.
        * - HistoricoChamado: histórico de alterações nos chamados.
        * - Anexos: arquivos anexados aos chamados.
        * - CriterioPrioridades: critérios de priorização de chamados.
        * - Prioridades: tabela de prioridades definidas.
        * - Cargos: tabela de cargos dos usuários.
        * - HistoricoUsuario: histórico de alterações em usuários.
    */

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Setor> Setores { get; set; }
    public DbSet<Sessao> Sessoes { get; set; }
    public DbSet<Chamado> Chamados { get; set; } // Tabela Chamados
    public DbSet<ChamadoComUsuario> ChamadosComUsuarios { get; set; } // Resultado do JOIN
    public DbSet<Comentario> Comentarios { get; set; }
    public DbSet<HistoricoChamado> HistoricoChamado { get; set; }
    public DbSet<Anexo> Anexos { get; set; }
    public DbSet<CriterioPrioridade> CriterioPrioridades { get; set; }
    public DbSet<Prioridades> Prioridades { get; set; }
    public DbSet<Cargo> Cargos { get; set; }
    public DbSet<HistoricoUsuario> HistoricoUsuario { get; set; }

    /**
        * OnModelCreating
        *
        * Configurações adicionais do modelo EF Core.
        *
        * Observações:
        * - ChamadoComUsuario não possui chave primária, então é configurado com HasNoKey().
    */
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // O resultado da consulta personalizada não tem chave
        modelBuilder.Entity<ChamadoComUsuario>().HasNoKey();
    }
}
