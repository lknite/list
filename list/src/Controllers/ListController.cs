using k8s;
using k8s.Autorest;
using k8s.Models;
using list.crd.list;
using list.K8sHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace list.Controllers
{
    [ApiController]
    [Route("list")]
    [Produces("application/json")]
    public class ListController : ControllerBase
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
        public async Task<IActionResult> Post(
                string task,
                string action,
                string total,
                string size,
                int priority = 3,
                int timeout = 30,
                List<Attr> attrs = null
            )
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            // create a list
            await zK8sList.Post(
                User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")),
                task,
                action,
                "pending",
                total,
                size,
                priority,
                "0",
                "0",
                timeout,
                attrs
                );

            return Ok();
        }
    }
}