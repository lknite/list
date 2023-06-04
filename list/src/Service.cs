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
            if (Environment.GetEnvironmentVariable("OIDC_USER_CLAIM") == null)
            {
                throw new Exception("Missing required environment variable: 'OIDC_USER_CLAIM'");
            }
        }
        public async void Start()
        {
            main.Start();
        }
    }
}
