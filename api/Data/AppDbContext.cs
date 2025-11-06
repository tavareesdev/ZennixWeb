using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZennixApi.Models;

namespace ZennixApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Setor> Setores { get; set; } = null!;
        public DbSet<Chamado> Chamados { get; set; }
        public DbSet<HistoricoChamado> HistoricoChamado { get; set; }
        public DbSet<Anexo> Anexos { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<UsuarioLoginResult> UsuarioLoginResults { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsuarioLoginResult>()
                .HasNoKey()
                .ToView(null); // não mapeia para tabela real
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Ativa logs detalhados e mostra os valores dos parâmetros
            optionsBuilder
                .EnableSensitiveDataLogging() //  Use apenas em ambiente de desenvolvimento
                .LogTo(Console.WriteLine, LogLevel.Information);
        }
    }
}
