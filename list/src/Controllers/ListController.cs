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
        /// get list of 'list' objects owned by user
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
                if (l.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))) {
                    result.Add(l.Metadata.Name);
                }
            }


            return Ok();
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
                List<Attr> attrs = null
            )
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            // create a list
            string id = await zK8sList.Post(
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

            // format object to return as json
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("id", id);

            return Ok(result);
        }
    }
}