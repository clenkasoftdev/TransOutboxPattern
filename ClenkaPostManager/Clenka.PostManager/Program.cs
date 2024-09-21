using Clenka.Common.Constants;
using Clenka.Common.MessageModels;
using Clenka.PostManager.Data;
using Clenka.PostManager.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

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

            ListenForIntegrationEvents(builder);

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


        private static void ListenForIntegrationEvents(WebApplicationBuilder builder)
        {
            // Listen for integration events
            var factory = new ConnectionFactory() { HostName = "localhost" } ;
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "test", exclusive: false);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                var dbContextOptions = new DbContextOptionsBuilder<PostServiceContext>()
                    .UseSqlServer(builder.Configuration.GetConnectionString("PostServiceDb"))
                    .Options;

                var dbContext = new PostServiceContext(dbContextOptions);

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);

                JToken jtoken = JToken.Parse(message);


               // var data1 = JObject.Parse(message);
               // var data = JsonConvert.DeserializeObject<UserEventMessage>(message);

                var data = JObject.Parse((string)jtoken);
                var type = ea.RoutingKey;

                //switch (type)
                //{
                //    case GlobalConstants.EXCHANGE_USER_ADD_EVENT:
                //        var userToAdd = new User
                //        {
                //            ID = data!.Id,
                //            Name = data.Name
                //        };
                //        dbContext.Users.Add(userToAdd);
                //        break;
                //    case GlobalConstants.EXCHANGE_USER_UPDATE_EVENT:
                //        var foundUser = dbContext.Users.Find(data.Id);
                //        foundUser.Name = data.Name;
                //        dbContext.Users.Update(foundUser);
                //        break;
                //}

                switch (type)
                {
                    case GlobalConstants.EXCHANGE_USER_ADD_EVENT:
                        var userToAdd = new User
                        {
                            UserID = data["id"].Value<int>(),
                            Name = data["name"].Value<string>()
                        };
                        dbContext.Users.Add(userToAdd);
                        break;
                    case GlobalConstants.EXCHANGE_USER_UPDATE_EVENT:
                        var foundUser = dbContext.Users.Find(data["id"].Value<int>());
                        foundUser.Name = data["name"].Value<string>();
                        dbContext.Users.Update(foundUser);
                        break;
                }
                dbContext.SaveChanges();

            };

            channel.BasicConsume(queue: GlobalConstants.EXCHANGE_USER_POSTSERVICE_QUEUE,
                            autoAck: true,
                            consumer: consumer);

        }
    }
}
