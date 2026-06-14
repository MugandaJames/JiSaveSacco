using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // We will map tables here later
        // public DbSet<Member> Members { get; set; }
        // public DbSet<User> Users { get; set; }
        // public DbSet<SavingsAccount> SavingsAccounts { get; set; }
        // public DbSet<SavingsTransaction> SavingsTransactions { get; set; }
    }
}