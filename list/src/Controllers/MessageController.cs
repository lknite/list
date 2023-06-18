using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace list.Controllers
{
    [ApiController]
    [Route("block")]
    [Produces("application/json")]
    public class MessageController : ControllerBase
    {
        /*
        /// <summary>
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public async Task<IActionResult> Get()
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            return Ok();
        }
        */

        /// <summary>
        ///  send list owner a notification
        /// </summary>
        /// <param name="event"></param>
        /// <param name="list"></param>
        /// <param name="block"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [HttpPost()]
        public async Task<IActionResult> Post(
                string @event,
                string list,
                string block,
                string index
            )
        {
            // debug
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));


            return Ok();
        }

    }
}