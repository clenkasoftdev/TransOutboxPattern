using Microsoft.EntityFrameworkCore;

namespace Clenka.UserService.Data
{
    public class UserServiceContext : DbContext
    {
        public UserServiceContext(DbContextOptions<UserServiceContext> options) : base(options)
        {
        }

        public DbSet<Entities.User> Users { get; set; }
    }
}
