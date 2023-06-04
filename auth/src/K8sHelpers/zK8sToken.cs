using lido.CustomResourceDefinitions;
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
using lido;


namespace lido.K8sHelpers
{
    public class zK8sToken
    {
        static String api = "token";
        static String group = "tradermanager.lido.aarr.xyz";
        static String version = "v1";
        static String plural = api + "s";

        public static GenericClient generic = new GenericClient(Globals.service.kubeclient, group, version, plural);

        public static async Task<string> Post(
                Guid api_key,
                string claims
            )
        {
            // parse claims
            JsonElement o = JsonSerializer.Deserialize<JsonElement>(claims);

            // new game instance
            var e = new lido.crd.token.CrdToken()
            {
                Kind = "Token",
                ApiVersion = group +"/" + version,
                Metadata = new V1ObjectMeta
                {
                    Name = api_key.ToString(),
                    //Name = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                    NamespaceProperty = Globals.service.kubeconfig.Namespace,
                },
                // spec
                Spec = new lido.crd.token.CrdTokenSpec
                {
                    token = new lido.crd.token.Token()
                    {
                        email = o.GetProperty("email").ToString(),
                        name = o.GetProperty("name").ToString(),
                        claims = claims,
                        api_key = api_key.ToString()
                    }
                }
            };
            // debug
            Console.WriteLine(JsonSerializer.Serialize(e, new JsonSerializerOptions { WriteIndented = true }));

            try
            {
                Console.WriteLine("creating CR {0}", e.Metadata.Name);
                var response = await Globals.service.kubeclient.CustomObjects.CreateNamespacedCustomObjectWithHttpMessagesAsync(
                    e,
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

            //Dictionary<string, string> result = new Dictionary<string, string>();
            //result.Add("game", g.Metadata.Name);

            return e.Metadata.Name.ToString();
        }
    }
}
