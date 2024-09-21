using Clenka.PostManager.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clenka.PostManager.Data
{
    public class PostServiceContext : DbContext
    {
        public PostServiceContext(DbContextOptions<PostServiceContext> options) : base(options)
        {
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<User>()
        //        .Property(p => p.ID)
        //        .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

        //    base.OnModelCreating(modelBuilder);
        //}

        public DbSet<Post> Posts { get; set; }
        public DbSet<User> Users { get; set; }
    }

}
