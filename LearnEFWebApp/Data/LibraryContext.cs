using LearnEFWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnEFWebApp.Data
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options)
            : base(options)
        {
            // do nothing for now
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
    }
}