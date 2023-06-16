using k8s;
using System.Text.Json;
using list.Managers.Client;

namespace list
{
    public class Service : IHostedService
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            main.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("***");
            Console.WriteLine("* TODO: Clean shutdown");

            return Task.CompletedTask;
        }
    }
}
