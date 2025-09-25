using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolBanks.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            // local dev default — change via appsettings or env var
            builder.UseSqlServer("Server=localhost;Database=SchoolBanksDb;Trusted_Connection=True;Encrypt=False;");
            return new AppDbContext(builder.Options);
        }
    }
}