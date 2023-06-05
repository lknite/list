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
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                    DateTime when = DateTimeOffset.FromUnixTimeMilliseconds(
                        long.Parse(b.Spec.block.when)
                        ).UtcDateTime;
                    Console.WriteLine("timestamp: " + when);

                    // if block has not timedout, then skip
                    if (when.AddSeconds(l.Spec.list.timeout) < DateTime.UtcNow)
                    {
                        continue;
                    }

                    // format object to return as json
                    result.Add("block", b.Metadata.Name);

                    // release semaphore lock
                    Globals.semaphore.Release();

                    return Ok(result);
                }
            }

            // get index block
            CrdBlock i = await zK8sBlock.generic.ReadNamespacedAsync<CrdBlock>(
                    Globals.service.kubeconfig.Namespace, list);

            // is there another block to allocate?
            if (Int32.Parse(i.Spec.block.index) == Int32.Parse(i.Spec.block.size)) {
                // no more blocks to hand out

                // release semaphore lock
                Globals.semaphore.Release();

                return StatusCode(StatusCodes.Status404NotFound);
            }

            // index & size of new block
            string index = i.Spec.block.index;
            string size = l.Spec.list.size;

            // last block is sometimes not full size, if this is the case for this block, adjust
            if ((Int32.Parse(index) + Int32.Parse(size)) > Int32.Parse(i.Spec.block.size))
            {
                size = (Int32.Parse(i.Spec.block.size) - Int32.Parse(index)).ToString();
            }

            // create block
            string block = await zK8sBlock.Post(
                (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")),
                index,
                size
                );

            // update index block
            i.Spec.block.index = (Int32.Parse(index) + Int32.Parse(size)).ToString();
            await zK8sBlock.generic.PatchNamespacedAsync<CrdBlock>(
                    new V1Patch(i.Spec, V1Patch.PatchType.MergePatch),
                    Globals.service.kubeconfig.Namespace, i.Metadata.Name);

            // format object to return as json
            result.Add("block", block);

            // release semaphore lock
            Globals.semaphore.Release();

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