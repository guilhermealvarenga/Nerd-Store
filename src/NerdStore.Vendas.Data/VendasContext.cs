using Microsoft.EntityFrameworkCore;
using NerdStore.Core.Data;
using NerdStore.Core.Messages;
using NerdStore.Vendas.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.EntityFrameworkCore.Design;

namespace NerdStore.Vendas.Data
{
    public class VendasContext : DbContext, IUnitOfWork
    {
        public VendasContext(DbContextOptions<VendasContext> options)
            : base(options)
        {

        }

        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItems { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }


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
            
            return await base.SaveChangesAsync() > 0;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(
                e => e.GetProperties().Where(p => p.ClrType == typeof(string))))
                property.Relational().ColumnType = "varchar(100)";

            modelBuilder.Ignore<Event>();

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(VendasContext).Assembly);

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys())) relationship.DeleteBehavior = DeleteBehavior.ClientSetNull;

            modelBuilder.HasSequence<int>("MinhaSequencia").StartsAt(1000).IncrementsBy(1);
            base.OnModelCreating(modelBuilder);
        }
    }

    public class DesignTimeVendasContextFactory : IDesignTimeDbContextFactory<VendasContext>
    {
        public VendasContext CreateDbContext(string[] args)
        {
            // TODO: Encapsular
            var pathInitialProject = "NerdStore.WebApp.MVC";
            var path = $"{Directory.GetParent(Directory.GetCurrentDirectory())}/{pathInitialProject}";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<VendasContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            builder.UseSqlServer(connectionString);
            return new VendasContext(builder.Options);
        }
    }
}
