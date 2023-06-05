using k8s.Autorest;
using k8s.KubeConfigModels;
using k8s.Models;
using k8s;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics; using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;


namespace list.K8sHelpers
{
    public class zK8sBlock
    {
        static String api = "block";
        static String group = "list.aarr.xyz";
        static String version = "v1";
        static String plural = api + "s";

        public static GenericClient generic = new GenericClient(Globals.service.kubeclient, group, version, plural);

        public static async Task<string> Post(
                string list,
                string owner,
                string index,
                string size
            )
        {
            // parse claims
            //JsonElement o = JsonSerializer.Deserialize<JsonElement>(claims);

            // new block instance
            var b = new list.crd.block.CrdBlock()
            {
                Kind = "Block",
                ApiVersion = group +"/" + version,
                Metadata = new V1ObjectMeta
                {
                    Name = list,
                    NamespaceProperty = Globals.service.kubeconfig.Namespace,
                },
                // spec
                Spec = new list.crd.block.CrdBlockSpec
                {
                    block = new list.crd.block.Block()
                    {
                        list = list,
                        owner = owner,
                        index = index,
                        size = size,
                        state = "",
                        when = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString()
                    }
                }
            };
            // debug
            Console.WriteLine(JsonSerializer.Serialize(b, new JsonSerializerOptions { WriteIndented = true }));

            try
            {
                Console.WriteLine("creating CR {0}", b.Metadata.Name);
                var response = await Globals.service.kubeclient.CustomObjects.CreateNamespacedCustomObjectWithHttpMessagesAsync(
                    b,
                    group, version,
                    Globals.service.kubeconfig.Namespace,
                    plural).ConfigureAwait(false);
            }
            catch (HttpOperationException httpOperationException) when (httpOperationException.Message.Contains("422"))
            {
                var phase = httpOperationException.Response.ReasonPhrase;
                var content = httpOperationException.Response.Content;
                Console.WriteLine("response content: {0}", content);
                Console.WriteLine("response phase: {0}", phase);
            }
            catch (HttpOperationException ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }


            return b.Metadata.Name.ToString();
        }
    }
}
