using list.Helpers;
using k8s;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace list
{
    public class Service
    {
        public ApiKeyManager am;

        //
        public KubernetesClientConfiguration kubeconfig;
        public Kubernetes kubeclient;

        public Service()
        {
            Console.WriteLine("Service()");
            try
            {
                // Load from the default kubeconfig on the machine. (dev)
                kubeconfig = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }
            catch
            {
                // Load service account from namespace where pod is running (prod)
                kubeconfig = KubernetesClientConfiguration.InClusterConfig();
            }

            // Use the config object to create a client.
            kubeclient = new Kubernetes(kubeconfig);


            // Check for required environment variable(s)
            List<string> required = new List<string>();
            required.Add("OIDC_ENDPOINT");
            required.Add("OIDC_CLIENT_ID");
            required.Add("OIDC_CLIENT_SECRET");
            required.Add("OIDC_CALLBACK");
            required.Add("OIDC_SCOPE");
            required.Add("OIDC_USER_CLAIM");
            foreach (string req in required)
            {
                if (Environment.GetEnvironmentVariable(req) == null)
                {
                    throw new Exception("Missing required environment variable: '"+ req +"'");
                }
            }
        }
        public async Task Start()
        {
            //
            am = new ApiKeyManager();
        }
    }
}
