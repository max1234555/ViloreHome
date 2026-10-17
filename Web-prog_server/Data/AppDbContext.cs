using Microsoft.EntityFrameworkCore;
using Web_prog_server.Models;

namespace Web_prog_server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        { }

        public DbSet<Item> Items => Set<Item>();
    }
}
