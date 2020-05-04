using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using NerdStore.Core.Communication.Mediator;
using NerdStore.Core.Data;
using NerdStore.Core.DomainObjects;
using NerdStore.Core.Messages;
using NerdStore.Pagamentos.Business;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NerdStore.Pagamentos.Data
{
    public class PagamentoContext : DbContext, IUnitOfWork
    {
        private readonly IMediatorHandler _mediatorHandler;
      
        public PagamentoContext(DbContextOptions<PagamentoContext> options,
                                IMediatorHandler rebusHandler)
            : base(options)
        {
            _mediatorHandler = rebusHandler; // HACK: Necessário para executar comando do EF no Mac OS.
            //_mediatorHandler = rebusHandler ?? throw new ArgumentNullException(nameof(rebusHandler));
        }

        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<Transacao> Transacoes { get; set; }


        public async Task<bool> Commit()
        {
            foreach (var entry in ChangeTracker.Entries().Where(entry => entry.Entity.GetType().GetProperty("DataCadastro") != null))
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("DataCadastro").CurrentValue = DateTime.Now;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Property("DataCadastro").IsModified = false;
                }
            }

            var sucesso = await base.SaveChangesAsync() > 0;
            if (sucesso) await _mediatorHandler.PublicarEventos(this);

            return sucesso;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(
                e => e.GetProperties().Where(p => p.ClrType == typeof(string))))
                property.Relational().ColumnType = "varchar(100)";

            modelBuilder.Ignore<Event>();

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PagamentoContext).Assembly);

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys())) relationship.DeleteBehavior = DeleteBehavior.ClientSetNull;
            base.OnModelCreating(modelBuilder);
        }
    }

    public class DesignTimePagamentoContextFactory : IDesignTimeDbContextFactory<PagamentoContext>
    {
        private readonly IMediatorHandler _mediatorHandler;

        public DesignTimePagamentoContextFactory() { }

        public DesignTimePagamentoContextFactory(IMediatorHandler mediatorHandler)
        {
            _mediatorHandler = mediatorHandler;
        }

        public PagamentoContext CreateDbContext(string[] args)
        {
            // TODO: Encapsular no Core
            var pathInitialProject = "NerdStore.WebApp.MVC";
            var path = $"{Directory.GetParent(Directory.GetCurrentDirectory())}/{pathInitialProject}";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<PagamentoContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            builder.UseSqlServer(connectionString);
            return new PagamentoContext(builder.Options, _mediatorHandler);
        }
    }
}
