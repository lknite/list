using k8s;
using k8s.Autorest;
using k8s.Models;
using list.crd.block;
using list.crd.list;
using list.CustomResourceDefinitions;
using list.Helpers;
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
        public async Task<IActionResult> Get(/*string? block = null*/)
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            /*
            // by default, if no listId is provided, return a listing of all lists owned by user & allowAnnonymous
            if (list == null)
            {
            */
                CustomResourceList<CrdBlock> blocks = await zK8sBlock.generic.ListNamespacedAsync<CustomResourceList<CrdBlock>>(Globals.service.kubeconfig.Namespace);

                List<Block> result = new List<Block>();
                foreach (CrdBlock item in blocks.Items)
                {
                    // only return blocks owned by user
                    if (item.Spec.block.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
                    {
                        result.Add(item.Spec.block);
                    }
                }


                return Ok(result);
            /*
            }

            // otherwise, if listId is provided, return all properties of list
            CrdBlock b = await zK8sBlock.generic.ReadNamespacedAsync<CrdBlock>(
                    Globals.service.kubeconfig.Namespace, list);

            return Ok(b.Spec.block);
            */
        }

        [HttpPatch()]
        public async Task<IActionResult> Patch(
                string block
            )
        {
            Console.WriteLine("Username: " + User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")));
            Console.WriteLine("Email: " + User.FindFirstValue("email"));

            /*
            CustomResourceList<CrdBlock> blocks = await zK8sBlock.generic.ListNamespacedAsync<CustomResourceList<CrdBlock>>(Globals.service.kubeconfig.Namespace);

            List<Block> result = new List<Block>();
            foreach (CrdBlock b in blocks.Items)
            {
                // only return blocks owned by user
                if (b.Spec.block.owner.Equals(User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"))))
                {
                    result.Add(b.Spec.block);
                }
            }
            */


            return Ok();
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
                Globals.semaphore.Release();

                return StatusCode(StatusCodes.Status403Forbidden);
            }

            // first, are there existing blocks which have timed out which we can hand out again?
            CustomResourceList<CrdBlock> blocks = await zK8sBlock.generic.ListNamespacedAsync<CustomResourceList<CrdBlock>>(Globals.service.kubeconfig.Namespace);

            Block block = new Block();
            foreach (CrdBlock b in blocks.Items)
            {
                // check that blocks are associated with this list
                if (b.Spec.block.list.Equals(list))
                {
                    // has the block timed out?
                    DateTime when = Timestamp.getUtcDateTimeFromTimestampInMilliseconds(
                        long.Parse(b.Spec.block.when)
                        );
                    Console.WriteLine("");
                    Console.WriteLine(" timestamp: " + when);
                    Console.WriteLine("timeout at: " + (when).AddSeconds(l.Spec.list.timeout));

                    // if the block has not timed out, then skip to next block
                    Console.WriteLine((when).AddSeconds(l.Spec.list.timeout) + " vs " + DateTime.UtcNow);
                    Console.WriteLine("compare: " + DateTime.Compare((when).AddSeconds(l.Spec.list.timeout), DateTime.UtcNow));
                    if (DateTime.Compare((when).AddSeconds(l.Spec.list.timeout), DateTime.UtcNow) < 0)
                    {
                        Console.WriteLine("   pending: " + (when).AddSeconds(l.Spec.list.timeout));
                        continue;
                    }
                    Console.WriteLine("  timedout: " + DateTime.UtcNow);

                    // update timestamp
                    b.Spec.block.when = Timestamp.getUtcTimestampInMilliseconds().ToString();

                    // patch block with updated timestamp
                    await zK8sBlock.generic.PatchNamespacedAsync<CrdBlock>(
                            new V1Patch(b, V1Patch.PatchType.MergePatch),
                            Globals.service.kubeconfig.Namespace,
                            b.Metadata.Name);

                    // release semaphore lock
                    Globals.semaphore.Release();

                    return Ok(b.Spec.block);
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
            block.when = Timestamp.getUtcTimestampInMilliseconds().ToString();
            block.list = list;
            block.owner = User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM"));
            block.index = index;
            block.size = size;

            string id = await zK8sBlock.Post(block);

            // update index block
            i.Spec.block.index = (Int32.Parse(index) + Int32.Parse(size)).ToString();
            await zK8sBlock.generic.PatchNamespacedAsync<CrdBlock>(
                    new V1Patch(i, V1Patch.PatchType.MergePatch),
                    Globals.service.kubeconfig.Namespace, i.Metadata.Name);

            // release semaphore lock
            Globals.semaphore.Release();

            return Ok(block);
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