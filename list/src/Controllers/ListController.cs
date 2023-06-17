using k8s;
using k8s.Autorest;
using k8s.Models;
using list.crd.block;
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
        public async Task<IActionResult> Get(string? listId = null)
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            // by default, if no listId is provided, return a listing of all lists owned by user & allowAnnonymous
            if (listId == null)
            {
                CustomResourceList<CrdList> lists = await zK8sList.generic.ListNamespacedAsync<CustomResourceList<CrdList>>(Globals.service.kubeconfig.Namespace);

                List<string> result = new List<string>();
                foreach (CrdList l in lists.Items)
                {
                    // only return lists owned by user
                    if (l.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
                    {
                        result.Add(l.Metadata.Name);
                    }
                    // or set as allowAnonymous
                    if (l.Spec.list.allowAnonymous)
                    {
                        result.Add(l.Metadata.Name);
                    }
                }


                return Ok(result);
            }

            // otherwise, if listId is provided, return all properties of list
            CrdList list = await zK8sList.generic.ReadNamespacedAsync<CrdList>(
                    Globals.service.kubeconfig.Namespace, listId);

            return Ok(list);
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
            List list = new List();
            list.owner = User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"));
            list.total = total;
            list.size = size;
            list.task = task;
            list.action = action;
            list.state = "pending";
            list.priority = priority;
            list.complete = "0";
            list.percent = "0";
            list.timeout = timeout;
            list.isPublic = isPublic;
            list.allowAnonymous = allowAnonymous;
            list.attrs = attrs;

            // create a list
            string id = await zK8sList.Post(list);

            // create an index block with list as the name
            Block block = new Block();
            block.list = id;
            block.owner = list.owner;
            block.index = "0";
            block.size = list.total;
            await zK8sBlock.Post(block, true);

            // send to websocket of all who are able to process this list (if anonymous then to all)
            Dictionary <string, string> result = new Dictionary<string, string>();
            result.Add("new", id);
            Globals.service.cm.SendToAll(JsonSerializer.Serialize(result));

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