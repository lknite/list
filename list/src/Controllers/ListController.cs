using k8s;
using k8s.Autorest;
using k8s.Models;
using list.crd.block;
using list.crd.list;
using list.CustomResourceDefinitions;
using list.K8sHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
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
        public async Task<IActionResult> Get(string? list = null)
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            // by default, if no listId is provided, return a listing of all lists owned by user & allowAnnonymous
            if (list == null)
            {
                CustomResourceList<CrdList> lists = await zK8sList.generic.ListNamespacedAsync<CustomResourceList<CrdList>>(Globals.service.kubeconfig.Namespace);

                List<string> result = new List<string>();
                foreach (CrdList item in lists.Items)
                {
                    // only return lists owned by user
                    if (item.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
                    {
                        result.Add(item.Metadata.Name);
                    }
                    // or set as allowAnonymous
                    if (item.Spec.list.allowAnonymous)
                    {
                        result.Add(item.Metadata.Name);
                    }
                }


                return Ok(result);
            }

            CrdList l = null;
            try
            {
                // otherwise, if listId is provided, return all properties of list
                l = await zK8sList.generic.ReadNamespacedAsync<CrdList>(
                        Globals.service.kubeconfig.Namespace, list);
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


            return Ok(l.Spec.list);
        }

        /// <summary>
        /// set list state 'suspend' or 'resume'
        /// </summary>
        /// <param name="list"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [HttpPatch()]
        public async Task<IActionResult> Patch(
            string list,
            string state
            )
        {
            // acquire semaphore lock
            Globals.semaphore.Wait();

            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            CrdList l = null;
            try
            {
                // only list owner is allowed to modify list
                l = await zK8sList.generic.ReadNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace, list);
                if (!l.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
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

                // release semaphore lock
                Globals.semaphore.Release();

                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }
            }

            // is state valid?
            if (!((l.Spec.list.state == "active") && (state == "suspend")
                || (l.Spec.list.state == "suspend") && (state == "resume")))
            {
                // release semaphore lock
                Globals.semaphore.Release();

                return StatusCode(StatusCodes.Status403Forbidden);
            }

            // update state
            l.Spec.list.state = state;
            if (state.Equals("resume"))
            {
                l.Spec.list.state = "active";
            }

            try
            {
                // patch list with updated state
                await zK8sList.generic.PatchNamespacedAsync<CrdList>(
                        new V1Patch(l, V1Patch.PatchType.MergePatch),
                        Globals.service.kubeconfig.Namespace,
                        l.Metadata.Name);
            }
            catch { }

            // release semaphore lock
            Globals.semaphore.Release();

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
            list.state = "active";
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
            result.Add("event", "active");
            result.Add("list", id);
            Globals.service.cm.SendToAll(JsonSerializer.Serialize(result));

            // remove 'new' notice sent to websocket, and return instead 'id' for rest call
            result.Remove("event");
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
            // acquire semaphore lock
            Globals.semaphore.Wait();

            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            try {
                // only list owner is allowed to delete list
                CrdList l = await zK8sList.generic.ReadNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace, list);
                if (!l.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")))) {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }

                // delete list
                await zK8sList.generic.DeleteNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace, list);
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

                // release semaphore lock
                Globals.semaphore.Release();

                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }
            }

            // release semaphore lock
            Globals.semaphore.Release();

            return Ok();
        }
    }
}