using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace list.Controllers
{
    [ApiController]
    [Route("block")]
    [Produces("application/json")]
    public class BlockController : ControllerBase
    {
        /// <summary>
        /// asdf
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public async Task<IActionResult> Get()
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));


            return Ok();
        }

        [HttpPost()]
        public async Task<IActionResult> Post()
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));


            return Ok();
        }
    }
}