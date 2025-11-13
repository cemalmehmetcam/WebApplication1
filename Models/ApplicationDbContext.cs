using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Define your DbSets here. For example:
        public DbSet<TextEntry> TextEntries { get; set; }
    }

    public class TextEntry
    {
        public int Id { get; set; }
        public string Content { get; set; }
    }
}