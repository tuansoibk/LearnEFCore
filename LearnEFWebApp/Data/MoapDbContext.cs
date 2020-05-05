using LearnEFWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnEFWebApp.Data
{
    public class MoapDbContext : DbContext
    {
        public MoapDbContext(DbContextOptions<MoapDbContext> options) : base(options)
        { }

        /// <summary>
        ///  This constructor is used only for testing (creating fake object).
        /// </summary>
        public MoapDbContext()
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        public virtual DbSet<Player> Players { get; set; }
        public virtual DbSet<Team> Teams { get; set; }
    }
}