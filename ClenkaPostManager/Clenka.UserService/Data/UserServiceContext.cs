using Clenka.UserService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clenka.UserService.Data
{
    public class UserServiceContext : DbContext
    {
        public UserServiceContext(DbContextOptions<UserServiceContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<OutboxEvent> OutboxEvents { get; set; }
    }
}
