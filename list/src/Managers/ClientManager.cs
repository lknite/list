using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using list.Helpers;

namespace list.Managers.Client
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public Client client { set; get; }
        public string message { set; get; }
    }
    public class OnConnectedEventArgs : EventArgs
    {
        public Client client { set; get; }
    }

    public class ClientManager
    {
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceivedEventHandler;
        public event EventHandler<OnConnectedEventArgs> OnConnectedEventHandler;
        ConcurrentDictionary<WebSocket, Client> clients = new ConcurrentDictionary<WebSocket, Client>();

        public bool skipIdentify;

        public ClientManager(bool skipIdentify = false)
        {
            Console.WriteLine("Instantiate ClientManager");

            this.skipIdentify = skipIdentify;
        }

        Client AddSocket(WebSocket webSocket, TaskCompletionSource<object> socketFinishedTcs)
        {
            Client client = new Client(webSocket, socketFinishedTcs);
            clients[client.webSocket] = client;

            return client;
        }

        public async void RemoveSocket(WebSocket webSocket)
        {
            // TODO: await webSocket.CloseAsync();
            clients.Remove(webSocket, out Client client);

            client.mutex.WaitOne();
            client.isClosing = true;
            client.mutex.ReleaseMutex();
        }

        public async void SendToAll(string signal)
        {
            foreach (WebSocket webSocket in clients.Keys)
            {
                //var buffer = Encoding.UTF8.GetBytes(signal);

                //Console.WriteLine("Sending to websocket owned by '" + client.User + "'");
                try
                {
                    Client client = clients[webSocket];

                    client.Send(signal);
                }
                catch (KeyNotFoundException ex)
                {
                    Console.WriteLine("(1a) KeyNotFoundException: " + ex.Message);
                }
                /*
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("(1b) WebSocketException: " + ex.Message);
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine("(1c) WebSocketException: " + ex.Message);
                }
                */
            }
        }

        public async void SendToTrader(Guid apiKey, string e)
        {
            //mutex.WaitOne();

            List<WebSocket> sockets = new List<WebSocket>();

            foreach (WebSocket webSocket in clients.Keys)
            {
                if (clients[webSocket].apiKey.Equals(apiKey))
                {
                    //var buffer = new byte[1024 * 4];
                    var buffer = Encoding.UTF8.GetBytes(e);

                    //Console.WriteLine("Sending to websocket owned by '" + client.User + "'");
                    try
                    {
                        if (webSocket.State == WebSocketState.Open)
                        {
                            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Console.WriteLine("(2a) WebSocketException: " + ex.Message);

                        // store to remove outside of loop
                        sockets.Add(webSocket);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine("(2b) WebSocketException: " + ex.Message);

                        // store to remove outside of loop
                        sockets.Add(webSocket);
                    }
                    catch (WebSocketException ex)
                    {
                        Console.WriteLine("(2c) WebSocketException: " + ex.Message);

                        // store to remove outside of loop
                        sockets.Add(webSocket);
                    }
                }
            }

            // If socket dropped, remove it
            foreach (WebSocket socket in sockets)
            {
                Console.WriteLine("Removing socket");
                clients.Remove(socket, out Client value);
            }

            //mutex.ReleaseMutex();
        }

        public async Task<bool> HandleWebRequest(HttpContext context, Func<Task> next)
        {
            //Console.WriteLine("Incoming connection ...");
            if (context.Request.Path == "/ws")
            {
                //Console.WriteLine("(a)");
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var socketFinishedTcs = new TaskCompletionSource<object>();

                    //Console.WriteLine("(b)");
                    Client client = AddSocket(webSocket, socketFinishedTcs);

                    // Inside background processing call 'TrySetResult' on socketfinishedTcs when finished with socket
                    //Console.WriteLine("(c)");
                    try
                    {
                        bool isIdentify = false;
                        var buffer = new byte[1024 * 4];
                        string message = string.Empty;

                        if (!skipIdentify && !isIdentify)
                        {
                            Console.WriteLine("(validate token)");
                            //isIdentify = await Identify(webSocket, System.Text.Encoding.Default.GetString(buffer).TrimEnd('\0'));

                            string api_key = context.Request.Query["api_key"];
                            Console.WriteLine("api_key = '" + api_key + "'");

                            // verify the api_key provided
                            try
                            {
                                isIdentify = true;

                                // look up the passed in api_key
                                JsonElement o = JsonSerializer.Deserialize<JsonElement>(
                                    await zApiToken.Identify(new Guid(api_key), Globals.service.kubeconfig.Namespace)
                                    );

                                // store provided apiKey (Identify checks that it is a valid guid)
                                client.apiKey = new Guid(api_key);

                                // if found, parse out the stored claims
                                List<Claim> claims = new List<Claim>();
                                foreach (JsonProperty property in o.EnumerateObject())
                                {
                                    claims.Add(new Claim(property.Name, property.Value.ToString()));
                                }

                                // add new claims identity
                                client.User.AddIdentity(new ClaimsIdentity(claims));

                                /*
                                // debug (get rid of this, less we need to keep it)
                                client.user = client.User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")).ToString();
                                client.email = client.User.FindFirstValue("email").ToString();
                                */

                                // debug output, user info
                                Console.WriteLine("api_key provided"
                                        + ", user: " + client.User.FindFirstValue(Environment.GetEnvironmentVariable("OIDC_USER_CLAIM")).ToString()
                                        + ", email: " + client.User.FindFirstValue("email").ToString());

                                // debug output, display all claims
                                foreach (Claim claim in client.User.Claims)
                                {
                                    Console.WriteLine("- " + claim.Type + ": " + claim.Value);
                                }
                            }
                            catch
                            {
                                client.Send("{\"error\":\"invalid api_key\"}");
                                throw new KeyNotFoundException();
                            }
                        }

                        // Raise event
                        OnConnectedEventArgs argsOnConnected = new OnConnectedEventArgs();
                        argsOnConnected.client = client;
                        Globals.service.cm.OnConnected(argsOnConnected);

                        //Console.WriteLine("(d)");
                        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        while (!result.CloseStatus.HasValue)
                        {
                            // if <= 2, ignore, assume ping, also: no ping allowed before identifying
                            if (Encoding.Default.GetString(buffer).TrimEnd('\0').Length <= 2)
                            {
                                Console.WriteLine("*** PING");
                            }
                            else if (skipIdentify || isIdentify)
                            {
                                // Append received data to message
                                message += Encoding.Default.GetString(buffer).TrimEnd('\0');

                                // Check if message is complete
                                if (result.EndOfMessage)
                                {
                                    MessageReceivedEventArgs args = new MessageReceivedEventArgs();
                                    args.client = client;
                                    args.message = message;
                                    OnMessageReceived(args);

                                    // Reset message
                                    message = string.Empty;
                                }
                            }

                            buffer = new byte[1024 * 4];
                            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        }
                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("HandleWebRequest: " + e.Message);
                    }

                    //socketFinishedTcs.TrySetResult(true);
                    //await socketFinishedTcs.Task;

                    Console.WriteLine("Removing socket");
                    RemoveSocket(webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await next();
            }

            return true;
        }
        public void AddMessageEventHandler(EventHandler<MessageReceivedEventArgs> eventHandler)
        {
            OnMessageReceivedEventHandler += eventHandler;
        }
        public void AddOnConnectedEventHandler(EventHandler<OnConnectedEventArgs> eventHandler)
        {
            OnConnectedEventHandler += eventHandler;
        }

        public virtual void OnMessageReceived(MessageReceivedEventArgs args)
        {
            EventHandler<MessageReceivedEventArgs> handler = OnMessageReceivedEventHandler;
            if (handler != null)
            {
                handler(this, args);
            }
        }
        public virtual void OnConnected(OnConnectedEventArgs args)
        {
            EventHandler<OnConnectedEventArgs> handler = OnConnectedEventHandler;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }

    public class Client
    {
        /*
        public string user = String.Empty;
        public string email = string.Empty;
        */
        public Guid apiKey = Guid.Empty;
        public ClaimsPrincipal User = new ClaimsPrincipal();

        public WebSocket webSocket;
        public TaskCompletionSource<object> socketFinishedTcs;

        public Mutex mutex = new Mutex();
        public bool isClosing = false;

        public Client(WebSocket webSocket, TaskCompletionSource<object> socketFinishedTcs)
        {
            this.webSocket = webSocket;
            this.socketFinishedTcs = socketFinishedTcs;

            /*
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

            socketFinishedTcs.TrySetResult(true);
            break;
            */
        }

        public void Send(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);

            Send(buffer);
        }

        public void Send(byte[] buffer)
        {
            mutex.WaitOne();

            try
            {
                if (!isClosing && webSocket.State == WebSocketState.Open)
                {
                    webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[ClientManager]::Client.Send: " + e.Message);
            }

            mutex.ReleaseMutex();
        }
    }
}
