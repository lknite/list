using lido.Helpers;
using k8s;

namespace lido
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

            //
            am = new ApiKeyManager();
        }
        public async Task Start()
        {
        }
    }
}
