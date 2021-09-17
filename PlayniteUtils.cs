using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace VXApp4Playnite
{

    public class GameEntry
    {
        public String Name { get; set; } = "";
        public String SortingName { get; set; } = "";
        public String Description { get; set; } = "";
        public String Notes { get; set; } = "";
        public String ReleaseDate { get; set; } = "";
        public int? UserScore { get; set; }
        public int? CriticScore { get; set; }
        public int? CommunityScore { get; set; }
        public String BackgroundFileName { get; set; } = "";
        public String CoverFileName { get; set; } = "";
        public List<String> Region { get; set; } = new List<String>();
        public List<String> Series { get; set; } = new List<String>();
        public List<String> Developers { get; set; } = new List<String>();
        public List<String> Publishers { get; set; } = new List<String>();
        public List<String> Features { get; set; } = new List<String>();
        public List<String> Genres { get; set; } = new List<String>();
        public List<String> Categories { get; set; } = new List<String>();
        public List<String> AgeRating { get; set; } = new List<String>();
        public List<String> Tags { get; set; } = new List<String>();
        public List<String> Source { get; set; } = new List<String>();
    }

    class PlayniteUtils
    {

        public static Guid LookupItemIdByName(IPlayniteAPI PlayniteApi,String item_type, String name)
        {
            List<dynamic> entries = new List<dynamic>();
            switch (item_type)
            {
                case "AgeRating":
                    foreach (AgeRating ar in PlayniteApi.Database.AgeRatings)
                    {
                        if (ar.Name == name) { return ar.Id; }
                    }
                    break;
                case "Regions":
                    foreach (Region r in PlayniteApi.Database.Regions)
                    {
                        if (r.Name == name) { return r.Id; }
                    }
                    break;
                case "Series":
                    foreach (Series s in PlayniteApi.Database.Series)
                    {
                        if (s.Name == name) { return s.Id; }
                    }
                    break;
                case "Developers":
                case "Publishers":
                    foreach (Company c in PlayniteApi.Database.Companies)
                    {
                        if (c.Name == name) { return c.Id; }
                    }
                    break;
                case "Features":
                    foreach (GameFeature f in PlayniteApi.Database.Features)
                    {
                        if(f.Name == name) { return f.Id; }
                    }
                    break;
                case "Genres":
                    foreach(Genre g in PlayniteApi.Database.Genres)
                    {
                        if(g.Name == name) { return g.Id; }
                    }
                    break;
                case "Categories":
                    foreach(Category c in PlayniteApi.Database.Categories)
                    {
                        if(c.Name == name) { return c.Id; }
                    }
                    break;
                case "Tags":
                    foreach(Tag t in PlayniteApi.Database.Tags)
                    {
                        if(t.Name == name) { return t.Id; }
                    }
                    break;
                case "Source":
                    foreach(GameSource gs in PlayniteApi.Database.Sources)
                    {
                        if(gs.Name == name) { return gs.Id; }
                    }
                    break;
                default:
                    break;
            }
            foreach (var entry in entries)
            {
                if (entry.Name == name)
                {
                    return entry.Id;
                }
            }
            // Create New If Not Found
            switch (item_type)
            {
                case "Regions":
                    Region rg = new Region(name);
                    PlayniteApi.Database.Regions.Add(rg);
                    return rg.Id;
                case "AgeRating":
                    AgeRating ar = new AgeRating(name);
                    PlayniteApi.Database.AgeRatings.Add(ar);
                    return ar.Id;
                case "Series":
                    Series sr = new Series(name);
                    PlayniteApi.Database.Series.Add(sr);
                    return sr.Id;
                case "Developers":
                case "Publishers":
                    Company cm = new Company(name);
                    PlayniteApi.Database.Companies.Add(cm);
                    return cm.Id;
                case "Features":
                    GameFeature ft = new GameFeature(name);
                    PlayniteApi.Database.Features.Add(ft);
                    return ft.Id;
                case "Genres":
                    Genre gn = new Genre(name);
                    PlayniteApi.Database.Genres.Add(gn);
                    return gn.Id;
                case "Categories":
                    Category ct = new Category(name);
                    PlayniteApi.Database.Categories.Add(ct);
                    return ct.Id;
                case "Tags":
                    Tag tg = new Tag(name);
                    PlayniteApi.Database.Tags.Add(tg);
                    return tg.Id;
                case "Source":
                    GameSource gs = new GameSource(name);
                    PlayniteApi.Database.Sources.Add(gs);
                    return gs.Id;
                default:
                    break;
            }
            return Guid.Empty;
        }

        public static Game ImportVXAppInfoData(IPlayniteAPI PlayniteApi, Game game)
        {
            String vxapp_info_path = Path.Combine(game.GameImagePath, "vxapp.info");
            if (!File.Exists(vxapp_info_path)) { return game; }
            GameEntry entry = JsonConvert.DeserializeObject<GameEntry>(File.ReadAllText(vxapp_info_path, Encoding.UTF8));
            if (!String.IsNullOrEmpty(entry.Name)) {game.Name = entry.Name;}
            if (!String.IsNullOrEmpty(entry.SortingName)) {game.SortingName = entry.SortingName; }
            if (!String.IsNullOrEmpty(entry.Description)){game.Description = entry.Description; }
            if (!String.IsNullOrEmpty(entry.Notes)){game.Notes = entry.Notes; }
            if (!String.IsNullOrEmpty(entry.ReleaseDate)){ game.ReleaseDate = Convert.ToDateTime(entry.ReleaseDate); }
            if (entry.UserScore != null) { game.UserScore = entry.UserScore; }
            if (entry.CriticScore != null) { game.CriticScore = entry.CriticScore; }
            if (entry.CommunityScore != null) { game.CommunityScore = entry.CommunityScore; }
            
            foreach (var r in entry.Region)
            {
                game.RegionId = LookupItemIdByName(PlayniteApi, "Regions", r);
            }
            foreach (var r in entry.Series)
            {
                game.SeriesId = LookupItemIdByName(PlayniteApi, "Series", r);
            }
            foreach (var r in entry.AgeRating)
            {
                game.AgeRatingId = LookupItemIdByName(PlayniteApi, "AgeRating", r);
            }

            foreach (var r in entry.Developers)
            {
                game.DeveloperIds.Add(LookupItemIdByName(PlayniteApi, "Developers", r));
            }
            foreach (var r in entry.Publishers)
            {
                game.PublisherIds.Add(LookupItemIdByName(PlayniteApi, "Publishers", r));
            }
            foreach (var r in entry.Features)
            {
                game.FeatureIds.Add(LookupItemIdByName(PlayniteApi, "Features", r));
            }
            foreach (var r in entry.Genres)
            {
                game.GenreIds.Add(LookupItemIdByName(PlayniteApi, "Genres", r));
            }
            foreach (var r in entry.Categories)
            {
                game.CategoryIds.Add(LookupItemIdByName(PlayniteApi, "Categories", r));
            }
            foreach (var r in entry.Tags)
            {
                game.TagIds.Add(LookupItemIdByName(PlayniteApi, "Tags", r));
            }
            foreach (var r in entry.Source)
            {
                game.SourceId = LookupItemIdByName(PlayniteApi, "Source", r);
            }

            if (!String.IsNullOrEmpty(entry.BackgroundFileName))
            {
                if (!String.IsNullOrEmpty(game.BackgroundImage))
                {
                    PlayniteApi.Database.RemoveFile(game.BackgroundImage);
                }
                game.BackgroundImage = PlayniteApi.Database.AddFile(Path.Combine(game.GameImagePath, entry.BackgroundFileName), game.Id);
            }
            if (!String.IsNullOrEmpty(entry.CoverFileName))
            {
                if (!String.IsNullOrEmpty(game.CoverImage))
                {
                    PlayniteApi.Database.RemoveFile(game.CoverImage);
                }
                game.CoverImage = PlayniteApi.Database.AddFile(Path.Combine(game.GameImagePath, entry.CoverFileName), game.Id);
            }
            return game;
        }

        public static String ExportVXAppInfoData(IPlayniteAPI PlayniteApi, Game game)
        {
            GameEntry entry = new GameEntry();
            
            if (game.ReleaseDate != null)
            {
                DateTime rd = (DateTime)game.ReleaseDate;
                entry.ReleaseDate = rd.ToString("MM/dd/yyyy");
            }

            entry.Name = game.Name;
            entry.SortingName = game.SortingName;

            entry.Description = game.Description;
            entry.Notes = game.Notes;
            entry.UserScore = game.UserScore;
            entry.CriticScore = game.CriticScore;
            entry.CommunityScore = game.CommunityScore;


            if (game.Region != null){ entry.Region.Add(game.Region.Name);}
            if(game.Series != null){ entry.Series.Add(game.Series.Name);}
            if(game.Developers != null) { foreach(var e in game.Developers) { entry.Developers.Add(e.Name); } }
            if(game.Publishers != null) { foreach(var e in game.Publishers) { entry.Publishers.Add(e.Name); } }
            if(game.Features != null) { foreach(var e in game.Features) { entry.Features.Add(e.Name); } }
            if(game.Genres != null) { foreach(var e in game.Genres) { entry.Genres.Add(e.Name); } }
            if(game.Categories != null) { foreach(var e in game.Categories) { entry.Categories.Add(e.Name); } }
            if(game.Tags != null) { foreach(var e in game.Tags) { entry.Tags.Add(e.Name); } }
            if (game.AgeRating != null) { entry.AgeRating.Add(game.AgeRating.Name); }
            if (game.Source != null) { entry.Source.Add(game.Source.Name); }
            if (!String.IsNullOrEmpty(game.BackgroundImage)) { entry.BackgroundFileName = "background"; }
            if (!String.IsNullOrEmpty(game.CoverImage)) { entry.CoverFileName = "cover"; }

            return JsonConvert.SerializeObject(entry, Formatting.Indented);
        }

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

        public static Tag GetOnDeviceTag(IPlayniteAPI PlayniteApi)
        {
            foreach(Tag tag in PlayniteApi.Database.Tags)
            {
                if(tag.Name == "OnDevice")
                {
                    return tag;
                }
            }
            return PlayniteApi.Database.Tags.Add("OnDevice");
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
            Thread _ithrd = new Thread(unused => AppInstaller(PlayniteApi, game, install_path));
            _ithrd.Start();
            return true;
        }

        private static void AppInstaller(IPlayniteAPI PlayniteApi, Game game, String install_path)
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
            if(game.TagIds == null)
            {
                game.TagIds = new List<Guid>();
            }
            game.TagIds.Add(GetOnDeviceTag(PlayniteApi).Id);
            PlayniteApi.Notifications.Add(new NotificationMessage("Install Notifier", $"{game.Name} Installed.", NotificationType.Info));
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
           
            foreach(var tag in game.Tags)
            {
                if(tag.Name == "OnDevice")
                {
                    game.TagIds.Remove(tag.Id);
                    break;
                }
            }
            
            PlayniteApi.Notifications.Add(new NotificationMessage("Uninstall Notifier", $"{game.Name} Uninstalled.", NotificationType.Info));
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

        public static Boolean OpenSaveDir(IPlayniteAPI PlayniteApi, Game game, string save_path)
        {
            string save_cache_path = Path.Combine(save_path, Utils.DeriveAppCode(game.GameImagePath));
            if (!Directory.Exists(save_cache_path))
            {
                Directory.CreateDirectory(save_cache_path);
            }
            Process.Start(save_cache_path);
            return true;
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
            string vxlauncher_path = "{PlayniteDir}\\v4p\\tools\\VXLauncher.exe";
            string vxlauncher_wd = "{PlayniteDir}\\v4p\\tools";
            UInt64 app_size = Utils.DirSize(new DirectoryInfo(path_to_vxapp));
            string app_size_text = Utils.FileSizeFormatter.FormatSize(app_size);
            string appconfig_path = Path.Combine(path_to_vxapp, "vxapp.config");
            if (!File.Exists(appconfig_path)) { return Guid.Empty; }
            // ImportVXAppInfoData
            dynamic config_entries = JsonConvert.DeserializeObject(File.ReadAllText(appconfig_path, Encoding.UTF8));
            Game game = new Game
            {
                GameImagePath = path_to_vxapp,
                PlatformId = LookupPlatform(PlayniteApi).Id,
                OtherActions = new ObservableCollection<GameAction>(),
                TagIds = new List<Guid>(),
                InstallDirectory = path_to_vxapp,
                IsInstalled = true,
                DeveloperIds = new List<Guid>(),
                PublisherIds = new List<Guid>(),
                FeatureIds = new List<Guid>(),
                GenreIds = new List<Guid>(),
                CategoryIds = new List<Guid>()
            };



            game = ImportVXAppInfoData(PlayniteApi, game);
            // If we got absolutely nothing from that metadata import, just load the name as the directory.
            if (String.IsNullOrEmpty(game.Name))
            {
                game.Name = Path.GetFileNameWithoutExtension(path_to_vxapp);
            }
            // If we didn't get a config on the other hand, there isn't much we can do.
            if (config_entries == null) { return Guid.Empty; }

            // If we didn't have a background or cover image specified, try to pull it from the app root.
            if (String.IsNullOrEmpty(game.BackgroundImage))
            {
                string[] background_paths = Directory.GetFiles(path_to_vxapp, "background*");
                if (background_paths.Count() > 0)
                {
                    game.BackgroundImage = PlayniteApi.Database.AddFile(background_paths[0], game.Id);
                }
            }

            if (String.IsNullOrEmpty(game.CoverImage))
            {
                string[] cover_paths = Directory.GetFiles(path_to_vxapp, "cover*");
                if (cover_paths.Count() > 0)
                {
                    game.CoverImage = PlayniteApi.Database.AddFile(cover_paths[0], game.Id);
                }

            }




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

            GameAction openSaveTask = new GameAction
            {
                Name = "[VX] Open Save Directory",
                Type = GameActionType.URL,
                Path = $"playnite://vxctrl/opensave/{game.Id}"
            };
            game.OtherActions.Add(openSaveTask);

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
