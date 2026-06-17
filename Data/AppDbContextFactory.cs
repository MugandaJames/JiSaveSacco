using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JiSaveSacco.API.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseMySql(
                "server=localhost;database=jisavedb;user=root;password=James12626;",
                new MySqlServerVersion(new Version(8, 0, 0))
            );

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}