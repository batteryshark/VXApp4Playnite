using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace VXApp4Playnite
{
    public class ReleaseEntry
    {
        public String tag { get; set; } = String.Empty;
        public String download_url { get; set; } = String.Empty;
        public String username { get; set; } = String.Empty;
        public String repository_name { get; set; } = String.Empty;
        public String destination_path { get; set; } = String.Empty;
    }

    public class VXApp4PlayniteSettings : ObservableObject
    {

        public string[] app_repositories { get; set; }
        public string local_app_path { get; set; } = string.Empty;
        public string tmp_path { get; set; } = string.Empty;
        public string save_path { get; set; } = string.Empty;
        public List<ReleaseEntry> repos { get; set; }
        public Boolean enable_backsplash { get; set; } = true;
        public Boolean enable_rrf { get; set; } = true;
        public Boolean debug_vxapp { get; set; } = false;

        // Ignored Properties
        [DontSerialize]
        public string fallback_app_path { get; set; } = string.Empty;
        [DontSerialize]
        public string fallback_tmp_path { get; set; } = string.Empty;
        [DontSerialize]
        public string fallback_save_path { get; set; } = string.Empty;
        [DontSerialize]
        public String path_repos { get; set; } = string.Empty;
        [DontSerialize]
        public String plugin_name { get; set; } = string.Empty;
        [DontSerialize]
        public String tools_path { get; set; } = string.Empty;

    }

    public class VXApp4PlayniteSettingsViewModel : ObservableObject, ISettings
    {
        private readonly VXApp4Playnite plugin;
        private VXApp4PlayniteSettings editingClone { get; set; }

        private VXApp4PlayniteSettings settings;
        public VXApp4PlayniteSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
        public VXApp4PlayniteSettingsViewModel()
        {
        }

        public VXApp4PlayniteSettingsViewModel(VXApp4Playnite plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<VXApp4PlayniteSettings>();

 

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
                Settings.repos = savedSettings.repos;
                Settings.local_app_path = savedSettings.local_app_path;
                Settings.tmp_path = savedSettings.tmp_path;
                Settings.save_path = savedSettings.save_path;
                if (savedSettings.app_repositories != null && savedSettings.app_repositories.Length > 0)
                {
                    Settings.path_repos = String.Join(";", savedSettings.app_repositories);
                }                
                Settings.app_repositories = savedSettings.app_repositories;
                Settings.enable_backsplash = savedSettings.enable_backsplash;
                Settings.enable_rrf = savedSettings.enable_rrf;
                Settings.debug_vxapp = savedSettings.debug_vxapp;
            }
            else
            {
                Settings = new VXApp4PlayniteSettings();
            }

            Settings.tools_path = Path.Combine(this.plugin.PlayniteApi.Paths.ApplicationPath, "v4p", "tools");

            Settings.fallback_app_path = Path.Combine(this.plugin.PlayniteApi.Paths.ApplicationPath, "v4p", "app");
            Directory.CreateDirectory(Settings.fallback_app_path);
            Settings.fallback_tmp_path = Path.Combine(this.plugin.PlayniteApi.Paths.ApplicationPath, "v4p", "tmp");
            Directory.CreateDirectory(Settings.fallback_tmp_path);
            Settings.fallback_save_path = Path.Combine(this.plugin.PlayniteApi.Paths.ApplicationPath, "v4p", "save");
            Directory.CreateDirectory(Settings.fallback_save_path);

            if (Settings.repos == null)
            {
                Settings.repos = new List<ReleaseEntry>();
                ReleaseEntry pdx_entry = new ReleaseEntry();
                pdx_entry.username = "batteryshark";
                pdx_entry.repository_name = "pdx";
                pdx_entry.destination_path = "tools\\pdxplugins";

                ReleaseEntry vxtn_entry = new ReleaseEntry();
                vxtn_entry.username = "batteryshark";
                vxtn_entry.repository_name = "VXTools.NET";
                vxtn_entry.destination_path = "tools";
                ReleaseEntry smoothie_entry = new ReleaseEntry();
                smoothie_entry.username = "batteryshark";
                smoothie_entry.repository_name = "Smoothie.NET";
                smoothie_entry.destination_path = "tools";

                ReleaseEntry v4p_entry = new ReleaseEntry();
                v4p_entry.username = "batteryshark";
                v4p_entry.repository_name = "VXApp4Playnite";
                v4p_entry.destination_path = "update";

                Settings.repos.Add(vxtn_entry);
                Settings.repos.Add(smoothie_entry);
                Settings.repos.Add(pdx_entry);
                Settings.repos.Add(v4p_entry);
            }
            if (Settings.local_app_path == string.Empty) { Settings.local_app_path = Settings.fallback_app_path; }
            if (Settings.tmp_path == string.Empty) { Settings.tmp_path = Settings.fallback_tmp_path; }
            if (Settings.save_path == string.Empty) { Settings.save_path = Settings.fallback_save_path; }
            Environment.SetEnvironmentVariable("VXPATH_APP", Settings.local_app_path, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VXPATH_TMP", Settings.tmp_path, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VXPATH_SAVE", Settings.save_path, EnvironmentVariableTarget.User);
            if (Settings.debug_vxapp)
            {
                Environment.SetEnvironmentVariable("PDXDBG", "1", EnvironmentVariableTarget.User);
            }
            else
            {
                Environment.SetEnvironmentVariable("PDXDBG", null, EnvironmentVariableTarget.User);
            }

        }

        public void EndEdit()
        {
            Settings.app_repositories = Settings.path_repos.Split(';');
            Environment.SetEnvironmentVariable("VXPATH_APP", Settings.local_app_path, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VXPATH_TMP", Settings.tmp_path, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VXPATH_SAVE", Settings.save_path, EnvironmentVariableTarget.User);
            if (Settings.debug_vxapp)
            {
                Environment.SetEnvironmentVariable("PDXDBG", "1", EnvironmentVariableTarget.User);
            }
            else
            {
                Environment.SetEnvironmentVariable("PDXDBG", null, EnvironmentVariableTarget.User);
            }
            plugin.SavePluginSettings(this.Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }


        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }
    }



  
    }
