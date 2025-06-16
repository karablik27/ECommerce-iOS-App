// PaymentsService/Data/PaymentsDbContext.cs
using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;

namespace PaymentsService.Data
{
    public class PaymentsDbContext : DbContext
    {
        public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
            : base(options) { }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // уникальный индекс по UserId
            builder.Entity<Account>()
                .HasIndex(a => a.UserId)
                .IsUnique();

            // RowVersion — concurrency token, генерируется БД при вставке и обновлении
            builder.Entity<Account>()
                .Property(a => a.RowVersion)
                .IsRowVersion()                   // помечаем как версию строки
                .ValueGeneratedOnAddOrUpdate()    // генерируется при INSERT/UPDATE
                .IsRequired(false);               // nullable в модели

            base.OnModelCreating(builder);
        }
    }
}
