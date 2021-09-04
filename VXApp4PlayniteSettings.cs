using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;

namespace VXApp4Playnite
{
    public class VXApp4PlayniteSettings : ISettings
    {
        public VXApp4PlayniteSettings() { }
        public void BeginEdit() { }
        public void CancelEdit() { }

        private readonly VXApp4Playnite plugin;

        public string[] app_repositories { get; set; }
        public string local_app_path { get; set; } = string.Empty;
        public string tmp_path { get; set; } = string.Empty;
        public string save_path { get; set; } = string.Empty;
        public string current_plugin_tag { get; set; } = string.Empty;
        public string current_tools_tag { get; set; } = string.Empty;
        public Boolean enable_backsplash { get; set; } = true;
        public Boolean enable_rrf { get; set; } = true;
        public Boolean debug_vxapp { get; set; } = false;

        // Ignored Properties
        [JsonIgnore]
        public string fallback_app_path { get; set; } = string.Empty;
        [JsonIgnore]
        public string fallback_tmp_path { get; set; } = string.Empty;
        [JsonIgnore]
        public string fallback_save_path { get; set; } = string.Empty;
        [JsonIgnore]
        public String path_repos { get; set; } = string.Empty;
        [JsonIgnore]
        public String plugin_name { get; set; } = string.Empty;

        public VXApp4PlayniteSettings(VXApp4Playnite plugin){
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            this.fallback_app_path = Path.Combine(this.plugin.PlayniteApi.Paths.ApplicationPath, "v4p", "app");
            Directory.CreateDirectory(this.fallback_app_path);
            this.fallback_tmp_path = Path.Combine(this.plugin.PlayniteApi.Paths.ApplicationPath, "v4p", "tmp");
            Directory.CreateDirectory(this.fallback_tmp_path);
            this.fallback_save_path = Path.Combine(this.plugin.PlayniteApi.Paths.ApplicationPath, "v4p", "save");
            Directory.CreateDirectory(this.fallback_save_path);

            // Load saved settings.
            VXApp4PlayniteSettings savedSettings = plugin.LoadPluginSettings<VXApp4PlayniteSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                this.local_app_path = savedSettings.local_app_path;
                this.tmp_path = savedSettings.tmp_path;
                this.save_path = savedSettings.save_path;
                this.path_repos = String.Join(";", savedSettings.app_repositories);
                this.app_repositories = savedSettings.app_repositories;
                this.enable_backsplash = savedSettings.enable_backsplash;
                this.enable_rrf = savedSettings.enable_rrf;
                this.debug_vxapp = savedSettings.debug_vxapp;
            }
            if (this.local_app_path == string.Empty) { this.local_app_path = this.fallback_app_path; }
            if(this.tmp_path == string.Empty) { this.tmp_path = this.fallback_tmp_path; }
            if(this.save_path == string.Empty) { this.save_path = this.fallback_save_path; }
            Environment.SetEnvironmentVariable("VXPATH_APP", this.local_app_path, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VXPATH_TMP", this.tmp_path, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VXPATH_SAVE", this.save_path, EnvironmentVariableTarget.User);
            if (this.debug_vxapp)
            {
                Environment.SetEnvironmentVariable("PDXDBG", "1", EnvironmentVariableTarget.User);
            }
            else
            {
                Environment.SetEnvironmentVariable("PDXDBG", null, EnvironmentVariableTarget.User);
            }
           
        }

        public void EndEdit(){
            app_repositories = path_repos.Split(';');
            Environment.SetEnvironmentVariable("VXPATH_APP", this.local_app_path, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VXPATH_TMP", this.tmp_path, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("VXPATH_SAVE", this.save_path, EnvironmentVariableTarget.User);
            if (this.debug_vxapp)
            {
                Environment.SetEnvironmentVariable("PDXDBG", "1", EnvironmentVariableTarget.User);
            }
            else
            {
                Environment.SetEnvironmentVariable("PDXDBG", null, EnvironmentVariableTarget.User);
            }
            plugin.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors){
            errors = new List<string>();
            return true;
        }
    }
}