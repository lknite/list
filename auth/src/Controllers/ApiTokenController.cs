using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace list.Controllers
{
    [ApiController]
    [Route("api/token")]
    [Produces("application/json")]
    public class ApiTokenController : ControllerBase
    {
        /// <summary>
        /// Get email associated with an api_key (internal only)
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [AllowAnonymous]
        //[ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet()]
        public async Task<IActionResult> Get(Guid guid)
        {
            String claims = String.Empty;

            Console.WriteLine("Guid: " + guid);

            try
            {
                claims = await Globals.service.am.get(guid);
            }
            catch
            {
                Console.WriteLine("not found");
            }

            Console.WriteLine("claims: "+ claims);
            JsonElement o = JsonSerializer.Deserialize<JsonElement>(claims);
            Console.WriteLine(JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = true }));

            return Ok(o);
        }
    }
}