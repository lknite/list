using list.crd.list;
using list.K8sHelpers;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace list.Controllers
{
    [ApiController]
    [Route("block")]
    [Produces("application/json")]
    public class MessageController : ControllerBase
    {
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

            CrdList l = null;
            try
            {
                // look up list
                l = await zK8sList.generic.ReadNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace, list);
                if (!l.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                // only someone allowed to participate in a list is allowed to send messages
            }
            catch (k8s.Autorest.HttpOperationException ex)
            {
                /*
                Console.WriteLine("** one **");
                */
                Console.WriteLine("StatusCode: " + ex.Response.StatusCode);
                /*
                Console.WriteLine("   Message: " + ex.Message);
                Console.WriteLine("      Data: " + ex.InnerException.Data);
                */

                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }
            }


            return Ok();
        }

    }
}