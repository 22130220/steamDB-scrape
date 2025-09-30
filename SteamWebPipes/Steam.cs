using Newtonsoft.Json;
using SteamKit2;
using SteamWebPipes.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SteamWebPipes
{

    public static class KeyValueExtensions
    {
        public static object ToDictionary(this KeyValue kv)
        {
            if (kv == null)
                return null;

            // Nếu có children -> convert đệ quy sang Dictionary
            if (kv.Children != null && kv.Children.Any())
            {
                var dict = new Dictionary<string, object>();
                foreach (var child in kv.Children)
                {
                    dict[child.Name] = child.ToDictionary();
                }
                return dict;
            }

            // Nếu chỉ có value
            return kv.Value;
        }
    }
    internal class Steam
    {
        public class SteamKitLogger : IDebugListener
        {
            public void WriteLine(string category, string msg)
            {
                Bootstrap.Log("[SteamKit] {0} {1}", category, msg);
            }
        }

        private readonly CallbackManager CallbackManager;
        private readonly SteamClient Client;
        private readonly SteamUser User;
        private readonly SteamApps Apps;
        private readonly SteamHelper SteamHelper;
        private bool IsLoggedOn;
        private uint TickerHash;

        public uint PreviousChangeNumber;
        public bool IsRunning = true;

        public Steam()
        {
            DebugLog.AddListener(new SteamKitLogger());
            DebugLog.Enabled = true;

            Client = new SteamClient();
            User = Client.GetHandler<SteamUser>();
            Apps = Client.GetHandler<SteamApps>();

            SteamHelper = new SteamHelper(Apps);

            CallbackManager = new CallbackManager(Client);
            CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            CallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            CallbackManager.Subscribe<SteamApps.PICSChangesCallback>(OnPICSChangesAsync);
        }

        public void Tick()
        {
            Client.Connect();

            var timeout = TimeSpan.FromSeconds(5);

            while (IsRunning)
            {
                CallbackManager.RunWaitCallbacks(timeout);
            }
        }

        private async Task ChangesTick()
        {
            var currentHash = TickerHash;
            var random = new Random();
            
            if (currentHash == 0)
            {
                Bootstrap.Log("Waiting a minute before requesting changes, to give a chance for users to reconnect");
                await Task.Delay(60000 + random.Next(10000));
            }
            else
            {
                Bootstrap.Log($"PICS ticker started #{currentHash}");
            }

            while (currentHash == TickerHash)
            {
                try
                {
                    await Apps.PICSGetChangesSince(PreviousChangeNumber, true, true);
                }
                catch (Exception e)
                {
                    Bootstrap.Log($"PICSGetChangesSince: {e.GetType().Name}: {e.Message}");
                }
                
                await Task.Delay(random.Next(3210));
            }

            Bootstrap.Log($"PICS ticker stopped #{currentHash}");
        }

        private async void OnPICSChangesAsync(SteamApps.PICSChangesCallback callback)
        {
            if (PreviousChangeNumber == callback.CurrentChangeNumber)
            {
                return;
            }

            Bootstrap.Log("Changelist {0} -> {1} ({2} apps, {3} packages)", PreviousChangeNumber, callback.CurrentChangeNumber, callback.AppChanges.Count, callback.PackageChanges.Count);

            PreviousChangeNumber = callback.CurrentChangeNumber;

            // Group apps and package changes by changelist, this will seperate into individual changelists
            var appGrouping = callback.AppChanges.Values.GroupBy(a => a.ChangeNumber);
            var packageGrouping = callback.PackageChanges.Values.GroupBy(p => p.ChangeNumber);

            // Join apps and packages back together based on changelist number
            var changeLists = Utils.FullOuterJoin(appGrouping, packageGrouping, a => a.Key, p => p.Key, (a, p, key) => new SteamChangelist
                {
                    ChangeNumber = key,
                    Apps = a.Select(x => x.ID),
                    Packages = p.Select(x => x.ID)
                },
                new EmptyGrouping<uint, SteamApps.PICSChangesCallback.PICSChangeData>(),
                new EmptyGrouping<uint, SteamApps.PICSChangesCallback.PICSChangeData>())
                .OrderBy(c => c.ChangeNumber);

            foreach (var changeList in changeLists)
            {
                Bootstrap.Broadcast(new ChangelistEvent(changeList));
            }

            var appIds = appGrouping
                .SelectMany(group => group.Select(change => change.ID))
                .Distinct()
                .ToList();

            // Lấy tất cả PackageID
            var packageIds = packageGrouping
                .SelectMany(group => group.Select(change => change.ID))
                .Distinct()
                .ToList();

            var productInfos = await SteamHelper.GetProductInfoAsync(appIds, packageIds);

            if (productInfos != null)
            {
                foreach (var info in productInfos)
                {
                    foreach (var kv in info.Apps)
                    {
                        var appId = kv.Key;
                        var appData = kv.Value;

                        var dict = new Dictionary<string, object>
                        {
                            ["AppID"] = appId,
                            ["Data"] = appData.KeyValues.ToDictionary()
                        };

                        string json = JsonConvert.SerializeObject(dict, Formatting.Indented);

                        Console.WriteLine(json);
                    }
                }
            }
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Bootstrap.Log("Connected to Steam, logging in...");

            User.LogOnAnonymous();
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            if (!IsRunning)
            {
                Bootstrap.Log("Shutting down...");

                return;
            }

            if (IsLoggedOn)
            {
                Bootstrap.Broadcast(new LogOffEvent());

                IsLoggedOn = false;
                TickerHash++;
            }

            Bootstrap.Log("Disconnected from Steam. Retrying...");

            Thread.Sleep(TimeSpan.FromSeconds(15));

            Client.Connect();
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Bootstrap.Log("Failed to login: {0}", callback.Result);

                Thread.Sleep(TimeSpan.FromSeconds(2));

                return;
            }

            IsLoggedOn = true;

            Bootstrap.Broadcast(new LogOnEvent());

            Bootstrap.Log("Logged in, current valve time is {0} UTC", callback.ServerTime);

            Task.Run(ChangesTick);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            if (IsLoggedOn)
            {
                Bootstrap.Broadcast(new LogOffEvent());

                IsLoggedOn = false;
                TickerHash++;
            }

            Bootstrap.Log("Logged off from Steam");
        }
    }
}
