using Clenka.UserService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace Clenka.UserService
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
            builder.Services.AddSwaggerGen( x =>
            {
                x.SwaggerDoc("v1", new OpenApiInfo { Title = "UserService", Version = "v1" });
            });

            var k = builder.Configuration.GetConnectionString("UserServiceDb");

            builder.Services.AddDbContext<UserServiceContext>(options =>
                           options.UseSqlServer(builder.Configuration.GetConnectionString("UserServiceDb")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                var scope = app.Services.CreateScope();
                var userCtx = scope.ServiceProvider.GetService<UserServiceContext>();
                userCtx.Database.EnsureCreated();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
