using Kursinis.Models;
using Microsoft.EntityFrameworkCore;

namespace Kursinis.Repository
{
    public class RepositoryContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
    }
}
