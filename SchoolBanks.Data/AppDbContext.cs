using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolBanks.Core;

namespace SchoolBanks.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Student> Students => Set<Student>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Student>()
                .HasIndex(s => s.StudentNumber)
                .IsUnique();

            mb.Entity<User>()
                .Property(u => u.Status)
                .HasConversion<string>(); // store enum as string "Active"/"Inactive"
        }
    }
}