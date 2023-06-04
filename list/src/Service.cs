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

        /*
        //
        static String api = "tic-tac-toe";
        //
        static String group = "list.aarr.xyz";
        static String version = "v1";
        static String plural = api + "s";

        //
        public GenericClient generic;
        */

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

            //
            //generic = new GenericClient(kubeclient, group, version, plural);
        }
        public async void Start()
        {
            main.Start();
        }

        /*
        // TODO: Put this in its own class somewhere
        public static void OnClientMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            // Parse and raise event with data
            JsonElement o = JsonSerializer.Deserialize<JsonElement>(args.message);

            Console.WriteLine("Message from client: "
                + JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Full lifetime management of websocket, returns upon dropped/closed websocket
        // Called from within Startup -> Configure -> app.Use
        public async Task<bool> HandleWebRequest(HttpContext context, Func<Task> next)
        {
            await cm.HandleWebRequest(context, next);
            return true;
        }
        */
    }
}
