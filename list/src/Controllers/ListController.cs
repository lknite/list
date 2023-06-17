using k8s;
using k8s.Autorest;
using k8s.Models;
using list.crd.list;
using list.CustomResourceDefinitions;
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
        /// get list of objects owned by user
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        public async Task<IActionResult> Get()
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            CustomResourceList<CrdList> lists = await zK8sList.generic.ListNamespacedAsync<CustomResourceList<CrdList>>(Globals.service.kubeconfig.Namespace);

            List<string> result = new List<string>();
            foreach (CrdList l in lists.Items)
            {
                // only return lists owned by user
                if (l.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")))) {
                    result.Add(l.Metadata.Name);
                }
            }


            return Ok(result);
        }


        /// <summary>
        /// create new list
        /// </summary>
        /// <returns></returns>
        [HttpPost()]
        public async Task<IActionResult> Post(
                string total,
                string size,
                string task = "",
                string action = "",
                int priority = 3,
                int timeout = 30,
                bool isPublic = true,
                bool allowAnonymous = false,
                List<Attr> attrs = null
            )
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            // create a list
            string list = await zK8sList.Post(
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
                isPublic,
                allowAnonymous,
                attrs
                );

            // create an index block with list as the name
            await zK8sBlock.Post(
                list,
                list,
                User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")),
                "0",
                total
                );

            // format object to return as json
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("list", list);

            // send to websocket of all who are able to process this list (if anonymous then to all)

            return Ok(result);
        }


        /// <summary>
        /// delete a list
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        [HttpDelete()]
        public async Task<IActionResult> Delete(string list)
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            // only list owner is allowed to delete list
            CrdList l = await zK8sList.generic.ReadNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace, list);
            if (!l.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")))) {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            await zK8sList.generic.DeleteNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace, list);


            return Ok();
        }
    }
}