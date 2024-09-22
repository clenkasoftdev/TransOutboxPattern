
using Clenka.Common.Constants;
using Clenka.PostManager.Data;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json.Linq;
using Clenka.PostManager.Entities;

namespace Clenka.PostManager
{
    public class MessageReceivingBackgroundService : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        private ILogger<MessageReceivingBackgroundService> _logger; 

        // constructor with arguments
        public MessageReceivingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MessageReceivingBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            using var scope = scopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<PostServiceContext>();
            dbContext.Database.EnsureCreated();

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _logger.LogInformation($"{typeof(MessageReceivingBackgroundService).Name} starting");
            while (!stoppingToken.IsCancellationRequested) {

                await HandleBackgroundService(stoppingToken);
            }

            _logger.LogInformation($"{typeof(MessageReceivingBackgroundService).Name} stopping");

        }

        private async Task HandleBackgroundService(CancellationToken stoppingToken)
        {
            try
            {
                // Listen for integration events
                var factory = new ConnectionFactory() { HostName = "localhost" };
                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                channel.QueueDeclare(queue: "test", exclusive: false);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (model, ea) =>
                {
                    using var scopeFactory = _scopeFactory.CreateScope();
                    using var dbContext = scopeFactory.ServiceProvider.GetRequiredService<PostServiceContext>();

                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] Received {0}", message);

                    JToken jtoken = JToken.Parse(message);

                    var data = JObject.Parse((string)jtoken);
                    var type = ea.RoutingKey;

                    int id = data["id"].Value<int>();
                    string name = data["name"].Value<string>();
                    int version = data["version"].Value<int>();

                    switch (type)
                    {
                        case GlobalConstants.EXCHANGE_USER_ADD_EVENT:
                            if (dbContext.Users.Any(o => o.ID == id))
                            {
                                Console.WriteLine($"This duplicate entry for user with id {id} shall be ignored");
                            }
                            else
                            {
                                var userToAdd = new User
                                {
                                    UserID = id,
                                    Name = name,
                                    Version = version
                                };

                                dbContext.Users.Add(userToAdd);

                            }

                            break;
                        case GlobalConstants.EXCHANGE_USER_UPDATE_EVENT:
                            int newVersion = version;
                            var foundUser = dbContext.Users.Find(id);
                            if (foundUser.Version >= version)
                            {
                                Console.WriteLine($"This duplicate entry for user with id {id} shall be ignored");
                            }
                            else
                            {
                                foundUser.Name = name;
                                foundUser.Version = version;
                                dbContext.Users.Update(foundUser);

                            }

                            break;
                    }
                    dbContext.SaveChanges();

                };

                channel.BasicConsume(queue: GlobalConstants.EXCHANGE_USER_POSTSERVICE_QUEUE,
                                autoAck: false,
                                consumer: consumer);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Post RabbitMq Receiver error {ex.Message}");
            }
        }
    }
}
