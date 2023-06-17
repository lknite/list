using k8s;
using System.Text.Json;
using list.Managers.Client;
using gge.K8sControllers;

namespace list
{
    public class Service : IHostedService
    {
        public ClientManager cm;
        public Main.Main main;

        //
        public KubernetesClientConfiguration kubeconfig;
        public Kubernetes kubeclient;

        //
        public ListK8sController listController;
        public BlockK8sController blockController;

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

            // Now that we have our kubeconfig, go ahead and instantiate the k8s controllers
            listController = new ListK8sController();
            blockController = new BlockK8sController();

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
            // save global reference for easy access
            Globals.service = this;

            //
            main.Start();

            // Start up all the k8s controllers
            listController.Listen();
            blockController.Listen();

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
