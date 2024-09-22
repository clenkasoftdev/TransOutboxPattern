using Clenka.UserService.Data;
using Microsoft.AspNetCore.Mvc;
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

            //The first line tells the service provider to create a singleton and give it to anyone who wants a BackgroundEventService like your controller's constructor.
            // However, the service provider is unaware that the singleton is actually an IHostedService and that you want it to call StartAsync()
            builder.Services.AddSingleton<BackgroundEventService>();

            //This line tells the service provider that you want to add a hosted service, so it'll call StartAsync() when the application starts running
            builder.Services.AddHostedService<BackgroundEventService>(provider => 
               provider.GetService<BackgroundEventService>()
            );

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
