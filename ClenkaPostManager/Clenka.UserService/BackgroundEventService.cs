
using Clenka.Common.Constants;
using Clenka.UserService.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Runtime;
using System.Text;

namespace Clenka.UserService
{
    public class BackgroundEventService : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        private ILogger<BackgroundEventService> _logger;
        public BackgroundEventService(IServiceScopeFactory serviceScopeFactory, ILogger<BackgroundEventService> logger)
        {
            _logger = logger;
            _scopeFactory = serviceScopeFactory;
            using var scope = _scopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceContext>();
            dbContext.Database.EnsureCreated();

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{typeof(BackgroundEventService).Name} starting");
            while (!stoppingToken.IsCancellationRequested)
            {
                await PublishOutboxEvents(stoppingToken);
            }
            _logger.LogInformation($"{typeof(BackgroundEventService).Name} stopping");
        }

        private async Task PublishOutboxEvents(CancellationToken cancToken)
        {
            try
            {
                var factory = new ConnectionFactory();
                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                channel.QueueDeclare(queue: "test", exclusive: false);

                while (!cancToken.IsCancellationRequested)
                {
                    {
                        using var scope = _scopeFactory.CreateScope();
                        using var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceContext>();

                        var outstandingEvents = dbContext.OutboxEvents.Where(o => o.ProcessedDate == null).OrderBy(o => o.ID).ToList();

                        foreach (var evt in outstandingEvents)
                        {
                            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evt.Data));
                            channel.BasicPublish(exchange: GlobalConstants.EXCHANGE_USER,
                                                                routingKey: evt.Event,
                                                                basicProperties: null,
                                                                body: body);
                            _logger.LogInformation($"Published {evt.Event} with Payload {evt.Data}");
                            evt.ProcessedDate = DateTime.Now;
                            dbContext.Entry(evt).State = EntityState.Modified;
                            dbContext.SaveChanges();
                        }
                    }
                    try
                    {
                        await Task.Delay(1000, cancToken);
                    }
                    catch (TaskCanceledException exception)
                    {
                        _logger.LogCritical(exception, "TaskCanceledException Error", exception.Message);
                    }

                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"{typeof(BackgroundEventService).Name} Exception with message: ", ex);
                await Task.Delay(5000, cancToken);
            }
        }
    }
}
