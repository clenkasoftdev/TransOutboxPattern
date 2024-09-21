using Clenka.PostManager.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clenka.PostManager.Data
{
    public class PostServiceContext : DbContext
    {
        public PostServiceContext(DbContextOptions<PostServiceContext> options) : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<User> Users { get; set; }
    }

}
