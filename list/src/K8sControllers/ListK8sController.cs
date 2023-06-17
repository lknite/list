using list;
using k8s.Models;
using k8s;
using System.Text.Json;
using list.crd.list;
using list.K8sHelpers;
using list.crd.block;
using list.CustomResourceDefinitions;

namespace gge.K8sControllers
{
    public class ListK8sController
    {
        static string api = "list";
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
                    await foreach (var (type, item) in generic.WatchNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace))
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
                            */
                            case WatchEventType.Deleted:
                                await Process(item);
                                break;
                            /*
                            case WatchEventType.Error:
                                break;
                            case WatchEventType.Modified:
                                await Process(item);
                                break;
                            */
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

        public async Task Process(CrdList l)
        {
            Dictionary<string, string> data;
            string patchStr = string.Empty;


            Console.WriteLine("Addition/Modify detected: " + l.Metadata.Name);
            Console.WriteLine("l.Spec.list.state: " + l.Spec.list.state);


            // loop through all blocks associated with deleted list & delete
            CustomResourceList<CrdBlock> blocks = await zK8sBlock.generic.ListNamespacedAsync<CustomResourceList<CrdBlock>>(
                    Globals.service.kubeconfig.Namespace);
            //Console.WriteLine("items.count: " + blocks.Items.Count());
            foreach (CrdBlock block in blocks.Items)
            {
                // is this block associated with the list?
                Console.WriteLine(block.Spec.block.list + " vs " + l.Metadata.Name);
                if (block.Spec.block.list.Equals(l.Metadata.Name))
                {
                    // if yes, then delete block 
                    //Console.WriteLine("delete block: " + block.Metadata.Name);
                    zK8sBlock.generic.DeleteNamespacedAsync<CrdBlock>(
                            Globals.service.kubeconfig.Namespace,
                            block.Metadata.Name);
                }
            }


            return;
        }
    }
}
