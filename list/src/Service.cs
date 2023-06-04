using k8s;
using System.Text.Json;
using list.Managers.Client;

namespace list
{
    public class Service
    {
        public ClientManager cm;
        public Main.Main main;

        //
        public KubernetesClientConfiguration kubeconfig;
        public Kubernetes kubeclient;

        public Service()
        {
            cm = new ClientManager();
            main = new Main.Main();

            try
            {
                // Load from the default kubeconfig on the machine.
                kubeconfig = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }
            catch
            {
                //
                kubeconfig = KubernetesClientConfiguration.InClusterConfig();
            }

            // Use the config object to create a client.
            kubeclient = new Kubernetes(kubeconfig);

            // Check for required environment variable(s)
            List<string> required = new List<string>();
            required.Add("OIDC_USER_CLAIM");
            foreach (string req in required)
            {
                if (Environment.GetEnvironmentVariable(req) == null)
                {
                    throw new Exception("Missing required environment variable: '" + req + "'");
                }
            }
        }
        public async void Start()
        {
            main.Start();
        }
    }
}
