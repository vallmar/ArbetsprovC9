using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ArbetsprovGysis.Models
{
    public class KonsultContext: DbContext
    {
        //public DbSet<Team> Teams { get; set; }
        public DbSet<Konsult> Konsult { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Konsult>().ToTable("Konsulter");
        }
        public KonsultContext(DbContextOptions options) : base(options)
        {
        }
    }
}
