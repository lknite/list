namespace list.Main
{
    public partial class Main
    {
        public static void ClientOnConnected(object sender, list.Managers.Client.OnConnectedEventArgs args)
        {
            Console.WriteLine("Event: ClientOnConnected");
        }
    }
}
