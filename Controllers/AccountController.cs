using System;
using System.Text;
using System.Threading.Tasks;
using FutbalMng.Auth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace FutbalMng.Auth.Controllers
{
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IModel _channel;

        public AccountController(UserManager<AppUser> userManager,
        IModel channel)
        {
            _userManager = userManager;
            _channel = channel;
        }

        [HttpPost]
        [Route("api/[controller]")]
        public async Task<IActionResult> Register([FromBody]RegisterRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new AppUser { UserId = Guid.NewGuid(), UserName = model.Email, Name = model.Name, Email = model.Email };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded) return BadRequest(result.Errors);
            
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("userName", user.UserName));
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("name", user.Name));
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("email", user.Email));
            // await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("role", Roles.Consumer));

            _channel.QueueDeclare(queue: "identity.user",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            
            var newUser = new {
                user.UserId,
                user.Name,
                user.Email,
                user.UserName,
            };

            var payload = JsonConvert.SerializeObject(newUser);
            var body = Encoding.UTF8.GetBytes(payload);

            _channel.ExchangeDeclare("BROKER_NAME", type: "direct", durable: true);
            var properties = _channel.CreateBasicProperties();

            properties.DeliveryMode = 2;

            _channel.BasicPublish(exchange: "BROKER_NAME",
                                 routingKey: "UserCreatedEvent",
                                 mandatory: true,
                                 basicProperties: properties,
                                 body: body);

            return Ok(new RegisterResponseViewModel(user));
        }

        public class RegisterRequestViewModel
        {
            public string Email { get; set; }
            public string Name { get; set; }
            public string Password { get; set; }
        }

        public class RegisterResponseViewModel
        {
            public RegisterResponseViewModel(AppUser user)
            {
                
            }
        }
    }
}