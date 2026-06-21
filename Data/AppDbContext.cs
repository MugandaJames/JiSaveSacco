using JiSaveSacco.API.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace JiSaveSacco.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Tables

        public DbSet<User> Users { get; set; }

        public DbSet<Member> Members { get; set; }

        public DbSet<SavingsTransaction> SavingsTransactions { get; set; }

        public DbSet<Loan> Loans { get; set; }

        public DbSet<LoanRepayment> LoanRepayments { get; set; }

        public DbSet<LoanSchedule> LoanSchedules { get; set; }

        public DbSet<Guarantor> Guarantors { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================
            // UNIQUE CONSTRAINTS
            // =====================

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Member>()
                .HasIndex(m => m.MemberNo)
                .IsUnique();

            modelBuilder.Entity<Member>()
                .HasIndex(m => m.NationalId)
                .IsUnique();

            // =====================
            // USER ↔ MEMBER (1 : 1)
            // =====================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Member)
                .WithOne(m => m.User)
                .HasForeignKey<Member>(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Member>()
                .HasIndex(m => m.UserId)
                .IsUnique();

            // =====================
            // RELATIONSHIPS
            // =====================

            modelBuilder.Entity<SavingsTransaction>()
                .HasOne(s => s.Member)
                .WithMany(m => m.SavingsTransactions)
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Member)
                .WithMany(m => m.Loans)
                .HasForeignKey(l => l.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoanRepayment>()
                .HasOne(r => r.Loan)
                .WithMany(l => l.LoanRepayments)
                .HasForeignKey(r => r.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoanSchedule>()
                .HasOne(ls => ls.Loan)
                .WithMany(l => l.LoanSchedules)
                .HasForeignKey(ls => ls.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Guarantor>()
                .HasOne(g => g.Loan)
                .WithMany(l => l.Guarantors)
                .HasForeignKey(g => g.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Guarantor>()
                .HasOne(g => g.Member)
                .WithMany(m => m.Guarantors)
                .HasForeignKey(g => g.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Member)
                .WithMany(m => m.Notifications)
                .HasForeignKey(n => n.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.GeneratedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = "Admin",
                    Status = "Active",
                    CreatedAt = new DateTime(2026, 1, 1)
                }
            );
        }
    }
}