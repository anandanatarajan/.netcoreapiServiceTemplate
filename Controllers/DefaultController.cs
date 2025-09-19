using Asp.Versioning;
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
        public DefaultController(ILogger<DefaultController> logger) 
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("DefaultController GET endpoint hit");
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
