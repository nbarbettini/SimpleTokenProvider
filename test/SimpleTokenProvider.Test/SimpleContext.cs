using Microsoft.EntityFrameworkCore;
using SimpleTokenProvider.Test.Models;

namespace SimpleTokenProvider.Test
{
    public class SimpleContext : DbContext
    {
        public SimpleContext(DbContextOptions<SimpleContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
