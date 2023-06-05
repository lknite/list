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
    [Route("block")]
    [Produces("application/json")]
    public class BlockController : ControllerBase
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

            CustomResourceList<CrdBlock> blocks = await zK8sBlock.generic.ListNamespacedAsync<CustomResourceList<CrdBlock>>(Globals.service.kubeconfig.Namespace);

            List<string> result = new List<string>();
            foreach (CrdBlock b in blocks.Items)
            {
                // only return blocks owned by user
                if (b.Spec.block.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
                {
                    result.Add(b.Metadata.Name);
                }
            }


            return Ok(result);
        }


        /// <summary>
        /// request a new block to work on
        /// </summary>
        /// <returns></returns>
        [HttpPost()]
        public async Task<IActionResult> Post(
                string list
            )
        {
            // acquire semaphore lock
            Globals.semaphore.Wait();

            // debug
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            // only list owner is allowed to create a new block
            CrdList l = await zK8sList.generic.ReadNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace, list);
            if (!l.Spec.list.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
            {
                // release semaphore lock
                Globals.semaphore.Wait();

                return StatusCode(StatusCodes.Status403Forbidden);
            }

            // first, are there existing blocks which have timed out which we can hand out again?
            CustomResourceList<CrdBlock> blocks = await zK8sBlock.generic.ListNamespacedAsync<CustomResourceList<CrdBlock>>(Globals.service.kubeconfig.Namespace);

            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (CrdBlock b in blocks.Items)
            {
                // check that blocks are associated with this list
                if (b.Spec.block.list.Equals(list))
                {
                    // has the block timed out?

                    // update the checkout timestamp

                    // format object to return as json
                    result.Add("block", "todo: id");

                    // release semaphore lock
                    Globals.semaphore.Wait();

                    return Ok(result);
                }
            }


            // create a block
            string index = "todo: index";
            string size = "todo: size";
            string id = await zK8sBlock.Post(
                list,
                User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")),
                index,
                size
                );

            // format object to return as json
            result.Add("block", id);

            // release semaphore lock
            Globals.semaphore.Wait();

            return Ok(result);
        }


        /*
        /// <summary>
        /// delete a block
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        [HttpDelete()]
        public async Task<IActionResult> Delete(string block)
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            // only block owner is allowed to delete block
            CrdBlock l = await zK8sBlock.generic.ReadNamespacedAsync<CrdBlock>(Globals.service.kubeconfig.Namespace, block);
            if (!l.Spec.block.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            await zK8sBlock.generic.DeleteNamespacedAsync<CrdBlock>(Globals.service.kubeconfig.Namespace, block);


            return Ok();
        }
        */
    }
}