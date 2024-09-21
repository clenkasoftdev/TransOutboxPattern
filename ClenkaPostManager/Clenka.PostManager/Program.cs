using Clenka.PostManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace Clenka.PostManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new OpenApiInfo { Title = "PostService", Version = "v1" });
            });

            builder.Services.AddDbContext<PostServiceContext>(options =>
                                      options.UseSqlServer(builder.Configuration.GetConnectionString("PostServiceDb")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                var scope = app.Services.CreateScope();
                var postCtx = scope.ServiceProvider.GetService<PostServiceContext>();
                postCtx.Database.EnsureCreated();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
