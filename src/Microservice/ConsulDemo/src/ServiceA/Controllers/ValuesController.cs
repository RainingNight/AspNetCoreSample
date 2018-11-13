using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ServiceA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<string>> Get()
        {
            await Task.Delay(1000);
            return "It's work from serviceA.";
        }
    }
}
