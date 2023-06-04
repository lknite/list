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
    [Route("")]
    [Produces("application/json")]
    public class ListController : ControllerBase
    {
        /// <summary>
        /// asdf
        /// </summary>
        /// <returns></returns>
        [HttpGet("list")]
        public async Task<IActionResult> Get()
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));


            return Ok();
        }

        [HttpPost("list")]
        public async Task<IActionResult> Post()
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));


            return Ok();
        }
    }
}