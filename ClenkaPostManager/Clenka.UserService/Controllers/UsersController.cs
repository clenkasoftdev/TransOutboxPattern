using Clenka.Common.Constants;
using Clenka.UserService.Data;
using Clenka.UserService.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Clenka.UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserServiceContext _context;

        public UsersController(UserServiceContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUser()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var integrationEventData = JsonConvert.SerializeObject(new
            {
                id = user.ID,
                name = user.Name,
            });

            PublishToMessageQueue(GlobalConstants.EXCHANGE_USER_UPDATE_EVENT, integrationEventData);

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var integrationEventData = JsonConvert.SerializeObject(new
            {
                id = user.ID,
                name = user.Name,
            });

            PublishToMessageQueue(GlobalConstants.EXCHANGE_USER_ADD_EVENT, integrationEventData);

            return CreatedAtAction("GetUser", new { id = user.ID }, user);
        }

        private void PublishToMessageQueue(string integrationEvent, string eventData)
        {
            // Publish to message queue
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();

            var channel = connection.CreateModel();
            channel.QueueDeclare(queue: "test",exclusive: false);
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventData));
            channel.BasicPublish(exchange: GlobalConstants.EXCHANGE_USER,
                                                routingKey: integrationEvent,
                                                basicProperties: null,
                                                body: body);
        }   
    }
}
