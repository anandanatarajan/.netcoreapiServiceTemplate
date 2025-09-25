using Asp.Versioning;
using Intellimix_Template.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupermarketRepository;

namespace Intellimix_Template.Controllers
{
    [EnableRateLimiting("fixed")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class DefaultController : ControllerBase
    {
        private readonly ILogger<DefaultController> _logger;
        private readonly Intellimix_Template.Messaging.IEventBus _eventBus;
        public DefaultController(ILogger<DefaultController> logger,IEventBus eventBus) 
        {
            _logger = logger;
            _eventBus = eventBus;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("DefaultController GET endpoint hit");
            _eventBus.PublishAsync<string>("DefaultController GET endpoint was called" );
            return Ok("API is running");
        }

        [HttpGet]
        [ApiVersion("2.0")]
        public IActionResult GetV2()
        {
            _logger.LogInformation("DefaultController GET endpoint hit for v2.0");
            return Ok("API is running - Version 2.0");
        }

        [HttpGet("test-error")]
        public IActionResult TestError()
        {
            try
            {
                throw new Exception("This is a test exception.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while testing.");
                return StatusCode(500, "Error logged.");
            }
        }

        private void check()
        {
            //DBClass dbc= new DBClass()
        }
    }
}
