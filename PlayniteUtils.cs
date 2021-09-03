using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VXApp4Playnite
{
    class PlayniteUtils
    {
        public static Game LookupGameByDBId(IPlayniteAPI PlayniteApi, string dbId)
        {
            foreach (var game in PlayniteApi.Database.Games)
            {
                if ((game.Id.ToString() == dbId))
                {
                    return game;
                }
            }
            PlayniteApi.Dialogs.ShowErrorMessage("Game with DatabaseId Not Found!", "Invalid DBId");
            return null;
        }

        public static Platform LookupPlatform(IPlayniteAPI PlayniteApi)
        {
            
            foreach (var platform in PlayniteApi.Database.Platforms)
            {
                if (platform.Name == "PC (VXApp)")
                {
                    return platform;
                }

            }
            return CreatePlatform(PlayniteApi);
        }

        public static Boolean IsAnyAppRunning(IPlayniteAPI PlayniteApi)
        {
            foreach (var game in PlayniteApi.Database.Games)
            {
                if(game.IsRunning || game.IsLaunching) { return true; }
            }
            return false;
        }
        public static Boolean LookupGameByDir(IPlayniteAPI PlayniteApi, string dir_name)
        {
            foreach (var game in PlayniteApi.Database.Games)
            {
                if ((game.GameImagePath.ToString().EndsWith(dir_name)) || (game.InstallDirectory.ToString().EndsWith(dir_name)))
                {
                    return true;
                }
            }
            return false;
        }

        public static Platform CreatePlatform(IPlayniteAPI PlayniteApi)
        {
            Platform np = new Platform("PC (VXApp)");
            PlayniteApi.Database.Platforms.Add(np);
            return np;
        }

     

        public static Boolean InstallGame(IPlayniteAPI PlayniteApi, Game game, string local_app_path)
        {
            if (!Directory.Exists(game.GameImagePath))
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Cannot Find Game Image Path!", "Invalid GameImagePath");
                return false;
            }
            game.IsInstalling = true;
            game.InstallDirectory = "";
            string install_path = Path.Combine(local_app_path, Path.GetFileName(game.GameImagePath));
            Thread _ithrd = new Thread(unused => AppInstaller(game, install_path));
            _ithrd.Start();
            return true;
        }

        private static void AppInstaller(Game game, String install_path)
        {
            Utils.CopyFilesRecursively(game.GameImagePath, install_path);
            game.IsInstalling = false;
            game.InstallDirectory = install_path;
            foreach (var action in game.OtherActions)
            {
                if (action.Name.Contains("Install App"))
                {
                    action.Name = action.Name.Replace("Install App", "Uninstall App");
                    action.Path = action.Path.Replace("vxctrl/install", "vxctrl/uninstall");
                }
            }
        }

        public static Boolean UninstallGame(IPlayniteAPI PlayniteApi, Game game, string local_app_path)
        {
            if (!Directory.Exists(game.InstallDirectory))
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Cannot Find Install Directory Path!", "Invalid InstallDirectory");
                game.IsInstalled = false;
                return false;
            }

            if (game.GameImagePath == game.InstallDirectory)
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Game install path is image path, cannot uninstall!", "Not Installed");
                return false;
            }

            string install_path = Path.Combine(local_app_path, Path.GetFileName(game.GameImagePath));
            game.IsUninstalling = true;
            Directory.Delete(game.InstallDirectory, true);
            game.IsUninstalling = false;
            game.InstallDirectory = game.GameImagePath;
            foreach (var action in game.OtherActions)
            {
                if (action.Name.Contains("Uninstall App"))
                {
                    action.Name = action.Name.Replace("Uninstall App", "Install App");
                    action.Path = action.Path.Replace("vxctrl/uninstall", "vxctrl/install");
                }

            }
            return true;
        }

        public static Boolean ClearSavedCache(IPlayniteAPI PlayniteApi, Game game, string save_path)
        {
            if (PlayniteApi.Dialogs.SelectString("Clearing the Cache for " + game.Name + " [" + Utils.DeriveAppCode(game.GameImagePath) + "] will erase all saved data. Please type \"YES\" to proceed.", "Are You Sure?", "").SelectedString == "YES")
            {
                string save_cache_path = Path.Combine(save_path, Utils.DeriveAppCode(game.GameImagePath));
                Directory.Delete(save_cache_path, true);
                return true;
            }
            return false;
        }

        public class AppInfo
        {
            public String Features { get; set; }
            public String Name { get; set; }
            public String Description { get; set; }
            public String Series { get; set; }
            public String Region { get; set; }
            public String Developer { get; set; }
            public String Publisher { get; set; }
            public object ReleaseYear { get; set; }
        }

        public class AppConfig
        {
            public String name { get; set; }
            public String map { get; set; }
            public String args { get; set; }
            public String cwd { get; set; }
            public String[] envars { get; set; }
            public String[] preload { get; set; }
        }

        public static Guid ImportGame(IPlayniteAPI PlayniteApi, string path_to_vxapp)
        {
            string vxlauncher_path = "{PlayniteDir}\\Extensions\\VXApp4Playnite\\tools\\VXLauncher.exe";
            string vxlauncher_wd = "{PlayniteDir}\\Extensions\\VXApp4Playnite\\tools";
            UInt64 app_size = Utils.DirSize(new DirectoryInfo(path_to_vxapp));
            string app_size_text = Utils.FileSizeFormatter.FormatSize(app_size);
            string appinfo_path = Path.Combine(path_to_vxapp, "vxapp.info");
            string appconfig_path = Path.Combine(path_to_vxapp, "vxapp.config");
            if (!File.Exists(appinfo_path)) { return Guid.Empty; }
            if (!File.Exists(appconfig_path)) { return Guid.Empty; }

            AppInfo app_info = JsonConvert.DeserializeObject<AppInfo>(File.ReadAllText(appinfo_path, Encoding.UTF8));
            dynamic config_entries = JsonConvert.DeserializeObject(File.ReadAllText(appconfig_path, Encoding.UTF8));
            if (app_info == null || config_entries == null) { return Guid.Empty; }

            Game game = new Game(app_info.Name);

            // Get Artwork Items and Add if We Have Them
            string[] background_paths = Directory.GetFiles(path_to_vxapp, "background*");
            string[] cover_paths = Directory.GetFiles(path_to_vxapp, "cover*");

            if (background_paths.Count() > 0)
            {
                game.BackgroundImage = PlayniteApi.Database.AddFile(background_paths[0], game.Id);
            }

            if (cover_paths.Count() > 0)
            {
                game.CoverImage = PlayniteApi.Database.AddFile(cover_paths[0], game.Id);
            }

            //game.PluginId = PluginId;
            game.OtherActions = new ObservableCollection<GameAction>();
            game.InstallDirectory = path_to_vxapp;
            game.GameImagePath = path_to_vxapp;
            game.IsInstalled = true;

            game.PlatformId = LookupPlatform(PlayniteApi).Id;
            game.Description = app_info.Description;
        
            // Set "Play" Action
            GameAction playTask = new GameAction
            {
                Name = "Play",
                Type = GameActionType.File,
                Path = vxlauncher_path,
                Arguments = "\"{InstallDir}\" config=\"" + config_entries[0].name + "\"",
                WorkingDir = vxlauncher_wd
            };
            game.PlayAction = playTask;


            // Set Install Action
            GameAction installTask = new GameAction
            {
                Name = $"[VX] Install App [{app_size_text}]",
                Type = GameActionType.URL,
                Path = $"playnite://vxctrl/install/{game.Id}"
            };

            game.OtherActions.Add(installTask);

            // Set Suspend Action
            GameAction suspendTask = new GameAction
            {
                Name = $"[VX] Suspend App",
                Type = GameActionType.URL,
                Path = $"playnite://vxctrl/suspend/{game.Id}"
            };
            game.OtherActions.Add(suspendTask);

            // Set Resume Action
            GameAction resumeTask = new GameAction
            {
                Name = $"[VX] Resume App",
                Type = GameActionType.URL,
                Path = $"playnite://vxctrl/resume/{game.Id}"
            };
            game.OtherActions.Add(resumeTask);

            // Set Close Action
            GameAction closeTask = new GameAction
            {
                Name = $"[VX] Close App",
                Type = GameActionType.URL,
                Path = $"playnite://vxctrl/close/{game.Id}"
            };
            game.OtherActions.Add(closeTask);

            // Set Force Close Action
            GameAction forceCloseTask = new GameAction
            {
                Name = "[VX] Force Close App",
                Type = GameActionType.File,
                Arguments = "\"{InstallDir}\" cmd=CLEANUP",
                Path = vxlauncher_path,
                WorkingDir = vxlauncher_wd
            };
            game.OtherActions.Add(forceCloseTask);

            // Set Clear Cache Action
            GameAction ccTask = new GameAction
            {
                Name = $"[VX] Clear Saved Data",
                Type = GameActionType.URL,
                Path = $"playnite://vxctrl/clearcache/{game.Id}"
            };
            game.OtherActions.Add(ccTask);

            foreach (var entry in config_entries)
            {
                GameAction ngaction = new GameAction
                {
                    Name = entry.name,
                    Type = GameActionType.File,
                    Arguments = "\"{InstallDir}\" config=\"" + entry.name + "\"",
                    Path = vxlauncher_path,
                    WorkingDir = vxlauncher_wd

                };
                game.OtherActions.Add(ngaction);
            }

            PlayniteApi.Database.Games.Add(game);
            return game.Id;
        }


    }
}
