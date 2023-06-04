using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace lido.Controllers
{
    [ApiController]
    [Authorize]
    [Route("/tradermanager/token")]
    [Produces("application/json")]
    public class TokenController : ControllerBase
    {
        /// <summary>
        /// Create a new instance of an api_key (Usually this would be a POST, but as a GET is easier for new folks.)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet()]
        public IActionResult Get(string name)
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            Dictionary<String, String> claims = new Dictionary<String, String>();
            claims.Add(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"),
                User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            claims.Add("email",
                User.FindFirstValue("email"));
            claims.Add("name", name);

            Console.WriteLine("Claims:\n" + JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true }));

            //Console.WriteLine("Claims:\n" + JsonSerializer.Serialize(User.Claims, new JsonSerializerOptions { WriteIndented = true }));

            //Guid guid = Globals.service.am.add(User.FindFirstValue("email"));
            Guid guid = Globals.service.am.add(
                JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true }));
            string result = "{\"api_key\":\"" + guid.ToString() + "\"}";

            JsonElement o = JsonSerializer.Deserialize<JsonElement>(result);
            Console.WriteLine(JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = true }));

            return Ok(o);
        }

        /// <summary>
        /// Create a new instance of an api_key (Also implemented as a GET for new folks.)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpPost()]
        public IActionResult Post(string name)
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            /*
            Dictionary<String, String> claims = new Dictionary<String, String>();
            claims.Add(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"),
                User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            claims.Add("email",
                User.FindFirstValue("email"));
            claims.Add("name", name);
            */
            Console.WriteLine("Claims:\n" + JsonSerializer.Serialize(User.Claims, new JsonSerializerOptions { WriteIndented = true }));

            //Guid guid = Globals.service.am.add(User.FindFirstValue("email"));
            Guid guid = Globals.service.am.add(
                JsonSerializer.Serialize(User.Claims, new JsonSerializerOptions { WriteIndented = true }));
            string result = "{\"api_key\":\"" + guid.ToString() + "\"}";

            JsonElement o = JsonSerializer.Deserialize<JsonElement>(result);
            Console.WriteLine(JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = true }));

            return Ok(o);
        }
    }
}