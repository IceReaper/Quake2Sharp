using Microsoft.AspNetCore.Mvc;

namespace QResourceServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ResourceController : ControllerBase
    {
        private readonly ILogger<ResourceController> _logger;

        public ResourceController(ILogger<ResourceController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            
        }
    }
}