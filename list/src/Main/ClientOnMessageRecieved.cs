using System.Text.Json.Nodes;
using System.Text.Json;

namespace list.Main
{
    public partial class Main
    {
        public static void ClientOnMessageReceived(object sender, list.Managers.Client.MessageReceivedEventArgs args)
        {
            Console.WriteLine("Event: ClientOnMessageReceived");
            Console.WriteLine("'" + args.message + "'");

            // testing, go ahead and forward to client if there is one
            //Globals.service.sm.SendToAll(args.message);

            //
            JsonElement o = JsonSerializer.Deserialize<JsonElement>(args.message);
        }
    }
}
