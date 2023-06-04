using list;
using System.Diagnostics;
using System.Text.Json;
using IdentityModel.Client;
using System.Text.Json.Nodes;

namespace list.Main
{
    public partial class Main
    {
        public Main()
        {
        }
        public void Start()
        {
            Globals.service.cm.OnMessageReceivedEventHandler += ClientOnMessageReceived;
            Globals.service.cm.OnConnectedEventHandler += ClientOnConnected;
        }
    }
}
