using k8s.Autorest;
using k8s.KubeConfigModels;
using k8s.Models;
using k8s;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection;
using list.crd.list;

namespace list.K8sHelpers
{
    public class zK8sList
    {
        static String api = "list";
        static String group = "list.aarr.xyz";
        static String version = "v1";
        static String plural = api + "s";

        public static GenericClient generic = new GenericClient(Globals.service.kubeclient, group, version, plural);

        public static async Task<string> Post(
                string owner,
                string task,
                string action,
                string state,
                string total,
                string size,
                int priority,
                string complete,
                string percent,
                int timeout,
                List<Attr> attrs
            )
        {
            // parse claims
            //JsonElement o = JsonSerializer.Deserialize<JsonElement>(claims);

            // new list instance
            var l = new list.crd.list.CrdList()
            {
                Kind = "List",
                ApiVersion = group +"/" + version,
                Metadata = new V1ObjectMeta
                {
                    Name = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                    NamespaceProperty = Globals.service.kubeconfig.Namespace,
                },
                // spec
                Spec = new list.crd.list.CrdListSpec
                {
                    list = new list.crd.list.List()
                    {
                        owner = owner,
                        task = task,
                        action = action,
                        state = state,
                        total = total,
                        size = size,
                        priority = priority,
                        complete = complete,
                        percent = percent,
                        timeout = timeout,
                        ts_add = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                        ts_start = "",
                        ts_suspend = "",
                        ts_resume = "",
                        ts_complete = "",
                        attrs = attrs
                    }
                }
            };
            // debug
            Console.WriteLine(JsonSerializer.Serialize(l, new JsonSerializerOptions { WriteIndented = true }));

            try
            {
                Console.WriteLine("creating CR {0}", l.Metadata.Name);
                var response = await Globals.service.kubeclient.CustomObjects.CreateNamespacedCustomObjectWithHttpMessagesAsync(
                    l,
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


            return l.Metadata.Name.ToString();
        }
    }
}
