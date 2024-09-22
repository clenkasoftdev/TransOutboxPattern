
using Clenka.Common.Constants;
using Clenka.UserService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private CancellationTokenSource _wakeupCancTokenSource = new CancellationTokenSource();

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

        public void StartPublishingOutsandingOutBoxEevnts()
        {
            // send a request to cancel
            _logger.LogInformation($"{typeof(BackgroundEventService).Name} request to start publishing cancellation event made");
            _wakeupCancTokenSource.Cancel();
        }

        private async Task PublishOutboxEvents(CancellationToken cancToken)
        {
            try
            {
                var factory = new ConnectionFactory();
                var connection = factory.CreateConnection();
                var channel = connection.CreateModel();
                channel.QueueDeclare(queue: "test", exclusive: false);

                // Enable publisher notification
                channel.ConfirmSelect();
                IBasicProperties props = channel.CreateBasicProperties(); // construct completely empty content header 
                props.DeliveryMode = 2; // 2 means persistent. 1 is non

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
                    // create a linked cancellation token that will be in the cancelled state when any of the source tokens are in a cancelled state
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_wakeupCancTokenSource.Token, cancToken);
                    try
                    {
                        await Task.Delay(Timeout.Infinite,linkedCts.Token);
                    }
                    catch (OperationCanceledException exception)
                    {
                        if (_wakeupCancTokenSource.Token.IsCancellationRequested)
                        {
                            _logger.LogInformation("Publising of Outbox Eevents requested");

                            //Initialise  new wakeup cancellation token and throw away the other one
                            var tmp = _wakeupCancTokenSource;
                            _wakeupCancTokenSource = new CancellationTokenSource();
                            tmp.Dispose();

                        }
                        else if (cancToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Shutting down service");
                        }
                    }

                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"{typeof(BackgroundEventService).Name} Exception with message: ", ex);
            }
        }
    }
}
