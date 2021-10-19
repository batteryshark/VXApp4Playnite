using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Events;
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

 

    public class VXApp4Playnite : GenericPlugin {

        public override Guid Id { get; } = Guid.Parse("b9e83df4-c46b-4877-9291-ba742c9eb142");
        private static readonly ILogger logger = LogManager.GetLogger();
        public String plugin_path;
        public static Boolean is_refreshing = false;
        public static BackSplash.BackSplash bs;



        public VXApp4PlayniteSettingsViewModel settings { get; set; }
        Thread tRepositoryMonitor;

        public static void RefreshLibrary(IPlayniteAPI PlayniteApi,VXApp4PlayniteSettings settings)
        {
            if (is_refreshing) { return; }
            is_refreshing = true;
            Dictionary<string, AppEntry> appcache = new Dictionary<string, AppEntry> { };
            Platform vxp = PlayniteUtils.LookupPlatform(PlayniteApi);

            // Take current inventory of current game db
            appcache.Clear();
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (game.IsUninstalling) { continue; }
                if (!game.PlatformIds[0].Equals(vxp.Id)) { continue; }

                AppEntry ne = new AppEntry
                {
                    isNew = false,
                    hasRepo = false,
                    imagePath = game.Roms[0].Path,
                    entryId = game.Id
                };

                if (appcache.ContainsKey(Path.GetFileName(game.Roms[0].Path)))
                {
                    appcache[Path.GetFileName(game.Roms[0].Path)] = ne;
                }
                else
                {
                    appcache.Add(Path.GetFileName(game.Roms[0].Path), ne);
                }
            }


            // Go through all repos and update cache states
            if (settings.app_repositories == null) { is_refreshing = false;  return; }
            if (settings.app_repositories.Length == 0) { is_refreshing = false; return; }
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
                    entry.entryId = PlayniteUtils.ImportGame(PlayniteApi, entry.newImagePath);
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
                        PlayniteApi.Database.Games[entry.entryId].Roms[0].Path = entry.newImagePath;
                        Game g = PlayniteApi.Database.Games[entry.entryId];
                        PlayniteApi.Database.Games.Update(g);
                    }

                    // If the imagePath no longer exists and it's the same as installPath, prune.
                    if ((!Directory.Exists(PlayniteApi.Database.Games[entry.entryId].Roms[0].Path)) || !entry.hasRepo)
                    {
                        if (PlayniteApi.Database.Games[entry.entryId].Roms[0].Path == PlayniteApi.Database.Games[entry.entryId].InstallDirectory)
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
            is_refreshing = false;
        }

        public static void RepositoryMonitor(object p)
        {
            
            VXApp4Playnite plugin = (VXApp4Playnite)p;

            while (true)
            {

                // We will let this thread chill if any app is running.
                if (!PlayniteUtils.IsAnyAppRunning(plugin.PlayniteApi))
                {
                    RefreshLibrary(plugin.PlayniteApi,plugin.settings.Settings);
                }
                // Wait to Update
                Thread.Sleep(60000);
            }
        }




        public VXApp4Playnite(IPlayniteAPI api) : base(api)
        {
           
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            settings = new VXApp4PlayniteSettingsViewModel(this);
            settings.Settings.plugin_name = "VXApp4Playnite" + "_" + this.Id.ToString();
            plugin_path = Path.Combine(PlayniteApi.Paths.ApplicationPath, "Extensions", settings.Settings.plugin_name);

            Directory.CreateDirectory(plugin_path);

            // Handling Post Update - Delete V4PUpdater.dll in this directory if it exists.
            String updater_library_path = Path.Combine(plugin_path, "V4PUpdater.dll");
            if (File.Exists(updater_library_path))
            {
                File.Delete(updater_library_path);
            }



            // URI Handler
            PlayniteApi.UriHandler.RegisterSource("vxctrl", (args) =>
            {
                Boolean op_status = false;
                var game = PlayniteUtils.LookupGameByDBId(this.PlayniteApi, args.Arguments[1]);
                if (game == null) { return; }

                switch (args.Arguments[0])
                {
                    case "suspend":
                        Utils.SendVXCommand(Utils.GetPipeName(game.Roms[0].Path), "SUSPEND");
                        break;
                    case "resume":
                        Utils.SendVXCommand(Utils.GetPipeName(game.Roms[0].Path), "RESUME");
                        break;
                    case "close":
                        Utils.SendVXCommand(Utils.GetPipeName(game.Roms[0].Path), "SHUTDOWN");
                        break;
                    case "clearcache":
                        op_status = PlayniteUtils.ClearSavedCache(PlayniteApi, game, settings.Settings.save_path);
                        break;
                    case "install":
                        op_status = PlayniteUtils.InstallGame(PlayniteApi, game, settings.Settings.local_app_path);
                        break;
                    case "uninstall":
                        op_status = PlayniteUtils.UninstallGame(PlayniteApi, game, settings.Settings.local_app_path);
                        break;
                    case "opensave":
                        op_status = PlayniteUtils.OpenSaveDir(PlayniteApi, game, settings.Settings.local_app_path);
                        break;
                    default:
                        break;
                }
            });

            bs = new BackSplash.BackSplash();

            if (settings.Settings.enable_rrf)
            {
                tRepositoryMonitor = new Thread(new ParameterizedThreadStart(RepositoryMonitor));
            }
            
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            if (settings.Settings.enable_rrf)
            {
                tRepositoryMonitor.Start(this);
            }
           
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            if (settings.Settings.enable_rrf)
            {
                tRepositoryMonitor.Abort();
            }
        }

       public override void OnGameStarting(OnGameStartingEventArgs args) { 
            
            if(args.Game.BackgroundImage != null && settings.Settings.enable_backsplash)
            {

                if (File.Exists(PlayniteApi.Database.GetFullFilePath(args.Game.BackgroundImage)))
                {
                    bs.Enable(PlayniteApi.Database.GetFullFilePath(args.Game.BackgroundImage));
                }
            }
            
        }

        public override void OnGameStarted(OnGameStartedEventArgs args) { 
            if (settings.Settings.enable_backsplash)
            {
                bs.Show();
            }        
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (settings.Settings.enable_backsplash)
            {
                bs.Disable();
            }
        }

        public static void SpawnRefreshLibrary(IPlayniteAPI PlayniteApi, VXApp4PlayniteSettings settings)
        {
            Thread t_refresh = new Thread(unused => RefreshLibrary(PlayniteApi, settings));
            t_refresh.Start();
        }

        public static void SpawnUpdaterThread(IPlayniteAPI PlayniteApi, VXApp4PlayniteSettingsViewModel settings, String plugin_path)
        {
            Thread t_updater = new Thread(unused => Updater.CheckForUpdates(PlayniteApi, settings, plugin_path));
            t_updater.Start();
        }

        public static void MetadataRefresher(IPlayniteAPI PlayniteApi)
        {
            List<Game> games = new List<Game>();
            foreach (var game in PlayniteApi.Database.Games)
            {
                games.Add(game);
            }
            PullRepositoryMetadata(PlayniteApi, games);
        }
        public static void SpawnMetaRefreshThread(IPlayniteAPI PlayniteApi)
        {
            Thread t_mrefresher = new Thread(unused => MetadataRefresher(PlayniteApi));
            t_mrefresher.Start();
        }

        public static void ShowAboutPopup(IPlayniteAPI PlayniteApi, VXApp4PlayniteSettings settings)
        {
            String about_msg = "VXApp4Playnite by BatteryShark\n";
            foreach(var entry in settings.repos)
            {
                about_msg += $"{entry.repository_name} Version {entry.tag}\n";
            }
            PlayniteApi.Dialogs.ShowMessage(about_msg);
        }

        public static void UpdateRepositoryMetadata(IPlayniteAPI PlayniteApi, List<Game> games)
        {
            Platform vxp = PlayniteUtils.LookupPlatform(PlayniteApi);
            foreach (Game g in games)
            {
                if (g.IsUninstalling) { continue; }
                if (!g.PlatformIds[0].Equals(vxp.Id)) { continue; }
                String vxapp_info_path = Path.Combine(g.Roms[0].Path, "vxapp.info");
                File.WriteAllText(vxapp_info_path, PlayniteUtils.ExportVXAppInfoData(PlayniteApi, g));
                File.Copy(PlayniteApi.Database.GetFullFilePath(g.BackgroundImage), Path.Combine(g.Roms[0].Path, "background"), true);
                File.Copy(PlayniteApi.Database.GetFullFilePath(g.CoverImage), Path.Combine(g.Roms[0].Path, "cover"), true);
            }
        }

        public static void PullRepositoryMetadata(IPlayniteAPI PlayniteApi, List<Game> games)
        {
            
               Platform vxp = PlayniteUtils.LookupPlatform(PlayniteApi);
            foreach (Game g in games)
            {                
                if (g.IsUninstalling) { continue; }
                if (!g.PlatformIds[0].Equals(vxp.Id)) { continue; }
                PlayniteApi.Database.Games.Update(PlayniteUtils.ImportVXAppInfoData(PlayniteApi, g));
            }  
        }

    // To add new main menu items override GetMainMenuItems
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs largs)
        {
            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    MenuSection = "VXApp4Playnite",
                    Description = "Refresh Library",
                    Action = (args) => SpawnRefreshLibrary(PlayniteApi,settings.Settings)
                },
                new MainMenuItem
                {
                    MenuSection = "VXApp4Playnite",
                    Description = "Refresh Metadata",
                    Action = (args) => SpawnMetaRefreshThread(PlayniteApi)
                },
                new MainMenuItem
                {
                    MenuSection = "VXApp4Playnite",
                    Description = "Check for Updates",
                    Action = (args) => SpawnUpdaterThread(PlayniteApi,settings,plugin_path)
                },
                new MainMenuItem
                {
                    MenuSection = "VXApp4Playnite",
                    Description = "About...",
                    Action = (args) => ShowAboutPopup(PlayniteApi,settings.Settings)
                }
            };
       }


        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs largs)
        {
            return new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Description = "[VX] Push Repository Metadata",
                    Action = (args) => {UpdateRepositoryMetadata(PlayniteApi,largs.Games); }
                },
                new GameMenuItem
                {
                    Description = "[VX] Pull Repository Metadata",
                    Action = (args) =>{ PullRepositoryMetadata(PlayniteApi,largs.Games); }
                },
            };
        }

        public override ISettings GetSettings(bool firstRunSettings){return settings;}
        public override UserControl GetSettingsView(bool firstRunSettings){return new VXAppPluginSettingsView(this);}
    }
}