using list;
using k8s;
using list.crd.list;
using list.K8sHelpers;
using list.crd.block;
using list.CustomResourceDefinitions;
using System.Net;
using k8s.Models;

namespace gge.K8sControllers
{
    public class BlockK8sController
    {
        static string api = "block";
        static string group = "list.aarr.xyz";
        static string version = "v1";
        static string plural = api + "s";

        static GenericClient generic = new GenericClient(Globals.service.kubeclient, group, version, plural);

        public async Task Listen()
        {
            // Enforce only processing one watch event at a time
            SemaphoreSlim semaphore;


            // Watch is a tcp connection therefore it can drop, use a while loop to restart as needed.
            while (true)
            {
                // Prep semaphore (reset in case of exception)
                semaphore = new SemaphoreSlim(1);

                Console.WriteLine("(" + api +") Listen begins ...");
                try
                {
                    await foreach (var (type, item) in generic.WatchNamespacedAsync<CrdBlock>(Globals.service.kubeconfig.Namespace))
                    {
                        Console.WriteLine("(event) [" + type + "] " + plural + "." + group + "/" + version + ": " + item.Metadata.Name);

                        // Acquire Semaphore
                        semaphore.Wait(Globals.cancellationToken);
                        Console.WriteLine("[" + item.Metadata.Name + "]");

                        // Handle event type
                        switch (type)
                        {
                            /*
                            case WatchEventType.Added:
                                await Process(item);
                                break;
                            case WatchEventType.Bookmark:
                                break;
                            case WatchEventType.Deleted:
                                await Process(item);
                                break;
                            case WatchEventType.Error:
                                break;
                            */
                            case WatchEventType.Modified:
                                await Process(item);
                                break;
                        }

                        // Release semaphore
                        Console.WriteLine("done.");
                        semaphore.Release();
                    }
                }
                catch (k8s.Autorest.HttpOperationException ex)
                {
                    Console.WriteLine("Exception? " + ex);
                    switch (ex.Response.StatusCode)
                    {
                        // crd is missing, sleep to avoid an error loop
                        case System.Net.HttpStatusCode.NotFound:
                            Console.WriteLine("crd is missing, pausing for a second before retrying");
                            Thread.Sleep(1000);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception occured while performing 'watch': " + ex);
                }
            }
        }

        public async Task Process(CrdBlock b)
        {
            Dictionary<string, string> data;
            string patchStr = string.Empty;


            Console.WriteLine("Addition/Modify detected: " + b.Metadata.Name);
            Console.WriteLine("b.Spec.block.state: " + b.Spec.block.state);

            // get list associated with block
            CrdList l = null;
            try
            {
                // otherwise, if listId is provided, return all properties of list
                l = await zK8sList.generic.ReadNamespacedAsync<CrdList>(
                        Globals.service.kubeconfig.Namespace, b.Spec.block.list);
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

                // abort, list not found
                return;
            }


            // merge blocks if possible

            // if all blocks are complete, set list to complete


            // temporary check for complete before merge is implemented
            long total = 0;

            // loop through all blocks associated with deleted list & delete
            CustomResourceList<CrdBlock> blocks = await zK8sBlock.generic.ListNamespacedAsync<CustomResourceList<CrdBlock>>(
                    Globals.service.kubeconfig.Namespace);
            foreach (CrdBlock block in blocks.Items)
            {
                // is this block associated with the list?
                Console.WriteLine(block.Spec.block.list + " vs " + b.Spec.block.list);
                if (block.Spec.block.list.Equals(b.Spec.block.list) && block.Spec.block.state.Equals("complete"))
                {
                    // if yes, add to total
                    total += long.Parse(block.Spec.block.size);
                }
            }

            // check if total is expected total
            if (total == long.Parse(l.Spec.list.total))
            {
                // set list as complete
                l.Spec.list.state = "complete";

                // patch list with updated state
                await zK8sList.generic.PatchNamespacedAsync<CrdList>(
                        new V1Patch(l, V1Patch.PatchType.MergePatch),
                        Globals.service.kubeconfig.Namespace,
                        l.Metadata.Name);
            }


            return;
        }
    }
}
