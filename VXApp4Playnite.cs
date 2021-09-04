using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;


namespace VXApp4Playnite
{
    class AppEntry
    {
        public Guid entryId { get; set; }
        public String imagePath { get; set; }
        public String newImagePath { get; set; }
        public Boolean isNew { get; set; }
        public Boolean hasRepo { get; set; }
    }

 

    public class VXApp4Playnite : Plugin {
        public override void OnGameInstalled(Game game) { }


        public override void OnGameUninstalled(Game game) { }
        public override void OnLibraryUpdated() { }
        public override Guid Id { get; } = Guid.Parse("b9e83df4-c46b-4877-9291-ba742c9eb142");
        private static readonly ILogger logger = LogManager.GetLogger();
        public String plugin_path;
        public String tools_path;
        public static BackSplash.BackSplash bs;


        
        public VXApp4PlayniteSettings settings { get; set; }
        Thread tRepositoryMonitor;

        public static void RefreshLibrary(IPlayniteAPI PlayniteApi,VXApp4PlayniteSettings settings)
        {
            Dictionary<string, AppEntry> appcache = new Dictionary<string, AppEntry> { };
            Platform vxp = PlayniteUtils.LookupPlatform(PlayniteApi);

            // Take current inventory of current game db
            appcache.Clear();
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.IsUninstalling) { continue; }
                if (!game.PlatformId.Equals(vxp.Id)) { continue; }

                AppEntry ne = new AppEntry
                {
                    isNew = false,
                    hasRepo = false,
                    imagePath = game.GameImagePath,
                    entryId = game.Id
                };

                if (appcache.ContainsKey(Path.GetFileName(game.GameImagePath)))
                {
                    appcache[Path.GetFileName(game.GameImagePath)] = ne;
                }
                else
                {
                    appcache.Add(Path.GetFileName(game.GameImagePath), ne);
                }
            }


            // Go through all repos and update cache states
            if (settings.app_repositories == null) {return; }
            if (settings.app_repositories.Length == 0) { return; }
            foreach (var repo in settings.app_repositories)
            {
                foreach (var d in Directory.GetDirectories(repo))
                {
                    if (!d.EndsWith(".vxapp")) { continue; }

                    AppEntry ne = new AppEntry
                    {
                        newImagePath = d,
                        hasRepo = true,
                        isNew = !PlayniteUtils.LookupGameByDir(PlayniteApi, Path.GetFileName(d))
                    };
                    if (appcache.ContainsKey(Path.GetFileName(d)))
                    {
                        appcache[Path.GetFileName(d)] = ne;
                    }
                    else
                    {
                        appcache.Add(Path.GetFileName(d), ne);
                    }
                }
            }


            foreach (var entry in appcache.Values)
            {

                // For all new entries, import them
                if (entry.isNew)
                {
                    // Import New Entry
                    // Make sure to put the new id into entry.entryId
                    entry.entryId = PlayniteUtils.ImportGame(PlayniteApi, entry.newImagePath, settings.plugin_name);
                    if (entry.entryId == Guid.Empty) { continue; }

                }
                else
                {
                    // We shouldn't do anything if the entry has been removed or it's running/installing.
                    if (PlayniteApi.Database.Games[entry.entryId] == null) { continue; }
                    if (PlayniteApi.Database.Games[entry.entryId].IsRunning) { continue; }
                    if (PlayniteApi.Database.Games[entry.entryId].IsInstalling) { continue; }

                    // If we got a new ImagePath, replace our old one.
                    if (Directory.Exists(entry.newImagePath))
                    {
                        PlayniteApi.Database.Games[entry.entryId].GameImagePath = entry.newImagePath;
                        Game g = PlayniteApi.Database.Games[entry.entryId];
                        PlayniteApi.Database.Games.Update(g);
                    }

                    // If the imagePath no longer exists and it's the same as installPath, prune.
                    if ((!Directory.Exists(PlayniteApi.Database.Games[entry.entryId].GameImagePath)) || !entry.hasRepo)
                    {
                        if (PlayniteApi.Database.Games[entry.entryId].GameImagePath == PlayniteApi.Database.Games[entry.entryId].InstallDirectory)
                        {
                            Game g = PlayniteApi.Database.Games[entry.entryId];
                            PlayniteApi.Database.Games.Remove(g);
                        }
                        else
                        { // If we have a different installPath, check to see if it's valid, if not, prune.
                            if (!Directory.Exists(PlayniteApi.Database.Games[entry.entryId].InstallDirectory))
                            {
                                PlayniteApi.Database.Games.Remove(entry.entryId);
                            }
                        }
                    }
                }
            }

        }

        public static void RepositoryMonitor(object p)
        {
            
            VXApp4Playnite plugin = (VXApp4Playnite)p;

            while (true)
            {

                // We will let this thread chill if any app is running.
                if (!PlayniteUtils.IsAnyAppRunning(plugin.PlayniteApi))
                {
                    RefreshLibrary(plugin.PlayniteApi,plugin.settings);
                }
                // Wait to Update
                Thread.Sleep(60000);
            }
        }




        public VXApp4Playnite(IPlayniteAPI api) : base(api)
        {
            settings = new VXApp4PlayniteSettings(this);
            settings.plugin_name = "VXApp4Playnite" + "_" + this.Id.ToString();
            plugin_path = Path.Combine(PlayniteApi.Paths.ApplicationPath, "Extensions", settings.plugin_name);
            tools_path = Path.Combine(plugin_path, "tools");
            Directory.CreateDirectory(plugin_path);
            Directory.CreateDirectory(tools_path);

            // URI Handler
            PlayniteApi.UriHandler.RegisterSource("vxctrl", (args) =>
            {
                Boolean op_status = false;
                var game = PlayniteUtils.LookupGameByDBId(this.PlayniteApi, args.Arguments[1]);
                if (game == null) { return; }

                switch (args.Arguments[0])
                {
                    case "suspend":
                        Utils.SendVXCommand(Utils.GetPipeName(game.GameImagePath), "SUSPEND");
                        break;
                    case "resume":
                        Utils.SendVXCommand(Utils.GetPipeName(game.GameImagePath), "RESUME");
                        break;
                    case "close":
                        Utils.SendVXCommand(Utils.GetPipeName(game.GameImagePath), "SHUTDOWN");
                        break;
                    case "clearcache":
                        op_status = PlayniteUtils.ClearSavedCache(PlayniteApi, game, settings.save_path);
                        break;
                    case "install":
                        op_status = PlayniteUtils.InstallGame(PlayniteApi, game, settings.local_app_path);
                        break;
                    case "uninstall":
                        op_status = PlayniteUtils.UninstallGame(PlayniteApi, game, settings.local_app_path);
                        break;
                    case "opensave":
                        op_status = PlayniteUtils.OpenSaveDir(PlayniteApi, game, settings.local_app_path);
                        break;
                    default:
                        break;
                }
            });

            bs = new BackSplash.BackSplash();

            if (settings.enable_rrf)
            {
                tRepositoryMonitor = new Thread(new ParameterizedThreadStart(RepositoryMonitor));
            }
            
        }

        public override void OnApplicationStarted()
        {
            if (settings.enable_rrf)
            {
                tRepositoryMonitor.Start(this);
            }
           
        }

        public override void OnApplicationStopped()
        {
            if (settings.enable_rrf)
            {
                tRepositoryMonitor.Abort();
            }
        }


        public override void OnGameStarting(Game game) {
            if(game.BackgroundImage != null && settings.enable_backsplash)
            {

                if (File.Exists(PlayniteApi.Database.GetFullFilePath(game.BackgroundImage)))
                {
                    bs.Enable(PlayniteApi.Database.GetFullFilePath(game.BackgroundImage));
                }
            }
            
        }

        public override void OnGameStarted(Game game) {
            if (settings.enable_backsplash)
            {
                bs.Show();
            }        
        }

        public override void OnGameStopped(Game game, long elapsedSeconds) {
            if (settings.enable_backsplash)
            {
                bs.Disable();
            }
        }


        // To add new main menu items override GetMainMenuItems
        public override List<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs largs)
        {
            return new List<MainMenuItem>
    {
        new MainMenuItem
        {
            MenuSection = "VXApp4Playnite",
            Description = "Refresh Library",
            Action = (args) => RefreshLibrary(PlayniteApi,settings)
        }
    };
        }

        public override ISettings GetSettings(bool firstRunSettings){return settings;}
        public override UserControl GetSettingsView(bool firstRunSettings){return new VXAppPluginSettingsView(this);}
    }
}