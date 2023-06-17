using list;
using k8s.Models;
using k8s;
using System.Text.Json;
using list.crd.list;

namespace gge.K8sControllers
{
    public class ListK8sController
    {
        static string api = "lists";
        static string group = "list.aarr.xyz";
        static string version = "v1";
        static string plural = api + "s";

        static GenericClient generic = new GenericClient(Globals.service.kubeclient, group, version, plural);

        public async Task Listen()
        {
            // Enforce only processing one watch event at a time
            SemaphoreSlim semaphore;


            // Watch is a tcp connection therefore it can drop, use a while loop to restart as needed.
            while (true)
            {
                // Prep semaphore (reset in case of exception)
                semaphore = new SemaphoreSlim(1);

                Console.WriteLine("(" + api +") Listen begins ...");
                try
                {
                    await foreach (var (type, item) in generic.WatchNamespacedAsync<CrdList>(Globals.service.kubeconfig.Namespace))
                    {
                        Console.WriteLine("(event) [" + type + "] " + plural + "." + group + "/" + version + ": " + item.Metadata.Name);

                        // Acquire Semaphore
                        semaphore.Wait(Globals.cancellationToken);
                        Console.WriteLine("[" + item.Metadata.Name + "]");

                        // Handle event type
                        switch (type)
                        {
                            /*
                            case WatchEventType.Added:
                                await Process(item);
                                break;
                            case WatchEventType.Bookmark:
                                break;
                            */
                            case WatchEventType.Deleted:
                                await Process(item);
                                break;
                            /*
                            case WatchEventType.Error:
                                break;
                            case WatchEventType.Modified:
                                await Process(item);
                                break;
                            */
                        }

                        // Release semaphore
                        Console.WriteLine("done.");
                        semaphore.Release();
                    }
                }
                catch (k8s.Autorest.HttpOperationException ex)
                {
                    Console.WriteLine("Exception? " + ex);
                    switch (ex.Response.StatusCode)
                    {
                        // crd is missing, sleep to avoid an error loop
                        case System.Net.HttpStatusCode.NotFound:
                            Console.WriteLine("crd is missing, pausing for a second before retrying");
                            Thread.Sleep(1000);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception occured while performing 'watch': " + ex);
                }
            }
        }

        public async Task Process(CrdList l)
        {
            Dictionary<string, string> data;
            string patchStr = string.Empty;


            Console.WriteLine("Addition/Modify detected: " + l.Metadata.Name);
            Console.WriteLine("g.Spec.state: " + l.Spec.list.state);

            /*
            // only work with new lists
            if (l.Spec.invite.state != "")
            {
                return;
            }
            */

            //
            switch (l.Spec.list.state)
            {
                /*
                case "accepted":
                    Console.WriteLine("processing 'accepted'");

                    // form data to append to event
                    data = new Dictionary<string, string>();
                    data.Add("email", i.Spec.invite.email);

                    // invite was accepted, generate event
                    await zK8sEvent.Post(
                        (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                        "",
                        0,
                        0,
                        "invite-accepted",
                        JsonSerializer.Serialize(data),
                        i.Spec.parent.template,
                        i.Spec.parent.game,
                        false
                        );

                    //
                    CrdGame g = await Globals.service.generic.ReadNamespacedAsync<CrdGame>("gge", i.Spec.parent.game.ToString());

                    // new player, generate event
                    Console.WriteLine("add player to game");
                    if (!i.Spec.invite.is_watcher)
                    {
                        // Add player to game
                        if (g.Spec.players == null)
                        {
                            g.Spec.players = new List<Player>();
                        }
                        // First make sure user is not already a player
                        bool found = false;
                        foreach (Player p in g.Spec.players)
                        {
                            if (p.email.Equals(i.Spec.invite.email))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            // possible race condition
                            break;
                        }
                        // Player not found, go ahead and add
                        g.Spec.players.Add(new Player()
                        {
                            user = i.Spec.invite.user,
                            email = i.Spec.invite.email,
                            algo = i.Spec.invite.algo,
                            id = g.Spec.players.Count + 1
                        });

                        // form data to append to event
                        data = new Dictionary<string, string>();
                        data.Add("email", i.Spec.invite.email);

                        // player was added, generate event
                        await zK8sEvent.Post(
                            (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                            "",
                            0,
                            0,
                            "player-add",
                            JsonSerializer.Serialize(data),
                            i.Spec.parent.template,
                            i.Spec.parent.game,
                            false
                            );
                    }
                    else
                    {
                        // Add watcher to game
                        if (g.Spec.watchers == null)
                        {
                            g.Spec.watchers = new List<Player>();
                        }
                        // First make sure user is not already a player
                        bool found = false;
                        foreach (Player p in g.Spec.watchers)
                        {
                            if (p.email.Equals(i.Spec.invite.email))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            // possible race condition
                            break;
                        }
                        // Watcher not found, go ahead and add
                        g.Spec.watchers.Add(new Player() {
                            user = i.Spec.invite.user,
                            email = i.Spec.invite.email,
                            algo = i.Spec.invite.algo,
                            id = g.Spec.watchers.Count + 1
                        });
                    }

                    // save the players and watchers before we save the invite as complete
                    patchStr = "{ \"spec\": {"
                                    + "\"players\": "
                                    + JsonSerializer.Serialize(g.Spec.players, new JsonSerializerOptions { WriteIndented = true })
                                    + ","
                                    + "\"watchers\": "
                                    + JsonSerializer.Serialize(g.Spec.watchers, new JsonSerializerOptions { WriteIndented = true })
                                    + "} }";
                    await Globals.service.generic.PatchNamespacedAsync<CrdGame>(
                            new V1Patch(patchStr, V1Patch.PatchType.MergePatch), "gge", g.Metadata.Name);

                    // Do we have two players?
                    if (g.Spec.players == null)
                    {
                        g.Spec.players = new List<Player>();
                    }
                    if (g.Spec.players.Count != 2)
                    {
                        // We do not have two players, don't start the game yet

                        // set state
                        i.Spec.invite.state = "complete";

                        break;
                    }

                    // Start game, we have our two players

                    // form data to append to event
                    data = new Dictionary<string, string>();

                    // game-staging, generate event
                    await zK8sEvent.Post(
                        (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                        "",
                        0,
                        0,
                        "game-staging",
                        JsonSerializer.Serialize(data),
                        i.Spec.parent.template,
                        i.Spec.parent.game,
                        false
                        );

                    // Let players know what their player id is
                    foreach (Player player in g.Spec.players)
                    {
                        // form data to append to event
                        Dictionary<string, int> dataPlayerId = new Dictionary<string, int>();
                        dataPlayerId.Add("id", player.id);

                        // game-staging, generate event
                        await zK8sEvent.Post(
                            (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                            "",
                            0,
                            player.id,
                            "player-id",
                            JsonSerializer.Serialize(dataPlayerId),
                            i.Spec.parent.template,
                            i.Spec.parent.game,
                            false
                            );
                    }


                    // set turn
                    g.Spec.turn = 1;

                    // set state
                    g.Spec.state = "active";

                    // save the turn counter and the new state of the game
                    patchStr = "{ \"spec\": {"
                                    + "\"turn\": "
                                    + g.Spec.turn
                                    + ","
                                    + "\"state\": "
                                    + "\"" + g.Spec.state + "\""
                                    + "} }";
                    await Globals.service.generic.PatchNamespacedAsync<CrdGame>(
                            new V1Patch(patchStr, V1Patch.PatchType.MergePatch), "gge", g.Metadata.Name);

                    // form data to append to event
                    data = new Dictionary<string, string>();

                    // game-staging, generate event
                    await zK8sEvent.Post(
                        (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                        "",
                        0,
                        0,
                        "game-start",
                        JsonSerializer.Serialize(data),
                        i.Spec.parent.template,
                        i.Spec.parent.game,
                        false
                        );

                    // form data to append to event
                    Dictionary<string, int> dataTurn = new Dictionary<string, int>();
                    dataTurn.Add("id", g.Spec.turn);

                    // game-staging, generate event
                    await zK8sEvent.Post(
                        (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                        "",
                        0,
                        0,
                        "turn",
                        JsonSerializer.Serialize(dataTurn),
                        i.Spec.parent.template,
                        i.Spec.parent.game,
                        false
                        );

                    // set state
                    i.Spec.invite.state = "complete";

                    break;
                case "rejected":
                    Console.WriteLine("processing 'rejected'");

                    // form data to append to event
                    data = new Dictionary<string, string>();
                    data.Add("email", i.Spec.invite.email);

                    // invite was rejected, generate event
                    await zK8sEvent.Post(
                        (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString(),
                        "",
                        0,
                        0,
                        "invite-rejected",
                        JsonSerializer.Serialize(data),
                        i.Spec.parent.template,
                        i.Spec.parent.game,
                        false
                        );

                    // set state
                    i.Spec.invite.state = "complete";

                    break;
                */
            }

            /*
            // if we finished processing the invite go ahead and mark it as complete
            // otherwise, in the case of an exception the invite will be processed again
            if (i.Spec.invite.state == "complete")
            {
                //
                patchStr = "{ \"spec\": { \"invite\": "
                    + JsonSerializer.Serialize(i.Spec.invite, new JsonSerializerOptions { WriteIndented = true })
                    + "} }";

                //Console.WriteLine("patchStr:\n" + patchStr);
                await generic.PatchNamespacedAsync<crd.invite.CrdInvite>(
                    new V1Patch(patchStr, V1Patch.PatchType.MergePatch), "gge", i.Metadata.Name);
            }
            */

            return;
        }
    }
}
