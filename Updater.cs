using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace VXApp4Playnite
{
    class Updater
    {

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        public static Boolean ProcessUpdate(VXApp4PlayniteSettings.ReleaseEntry update, String destination_path)
        {
            if (!Directory.Exists(destination_path))
            {
                Directory.CreateDirectory(destination_path);
            }
            string zip_path = Path.Combine(destination_path, "tmp.zip");
            string zip_tmp_path = Path.Combine(destination_path, "tmp");
            if (!Directory.Exists(zip_tmp_path))
            {
                Directory.CreateDirectory(zip_tmp_path);
            }
            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile(update.download_url, zip_path);
            }
            if (!File.Exists(zip_path)) { return false; }

            ZipFile.ExtractToDirectory(zip_path, zip_tmp_path);
            File.Delete(zip_path);
            CopyFilesRecursively(zip_tmp_path, destination_path);
            Directory.Delete(zip_tmp_path, true);
            
            return true;
        }

        public static Boolean GetLatestReleaseTag(VXApp4PlayniteSettings.ReleaseEntry entry)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"https://api.github.com/repos/{entry.username}/{entry.repository_name}/releases");
            request.ContentType = "application/json; charset=utf-8";
            request.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";

            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    dynamic releases = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    if (releases[0].tag_name == entry.tag) { return false; }
                    entry.tag = releases[0].tag_name;
                    entry.download_url = releases[0].assets[0].browser_download_url;

                    return true;

                }
            }
            catch { }
            return false;
        }

        public static void PrepPluginUpdate(String update_path, String plugin_path)
        {

            // Modify our yaml script to point at the new name.
            String yaml_from_current_plugin = Path.Combine(plugin_path, "extension.yaml");
            String yaml_from_update  = Path.Combine(update_path, "extension.yaml");
            // Modify our new yaml to point at the updater dll.
            String yaml_text = File.ReadAllText(yaml_from_update);
            yaml_text = yaml_text.Replace("Module: VXApp4Playnite.dll", "Module: V4PUpdater.dll");
            File.WriteAllText(yaml_from_current_plugin, yaml_text);
            // Move our Updater Library into the plugins directory.
            File.Move(Path.Combine(update_path, "V4PUpdater.dll"), Path.Combine(plugin_path, "V4PUpdater.dll"));
        }

        public static void CheckForUpdates(IPlayniteAPI PlayniteApi, VXApp4PlayniteSettings settings, String plugin_path)
        {
            Boolean at_least_one_update = false;
            String update_info = "Updated: ";
            Boolean plugin_updated = false;
            String plugin_update_path = "";
            List<VXApp4PlayniteSettings.ReleaseEntry> test_updates = new List<VXApp4PlayniteSettings.ReleaseEntry>(settings.repos);
            foreach(int i in Enumerable.Range(0,settings.repos.Count()))
            {
                try
                {
                    if (GetLatestReleaseTag(test_updates[i]))
                    {
                        String dest_path = Path.Combine(PlayniteApi.Paths.ApplicationPath, "v4p", test_updates[i].destination_path);

                        if (ProcessUpdate(test_updates[i], dest_path))
                        {
                            if(test_updates[i].repository_name == "VXApp4Playnite")
                            {
                                plugin_updated = true;
                                plugin_update_path = dest_path;
                            }
                            settings.repos[i].download_url = test_updates[i].download_url;
                            settings.repos[i].tag = test_updates[i].tag;
                            at_least_one_update = true;
                            update_info += $"{settings.repos[i].repository_name} [{settings.repos[i].tag}]\n";
                        }
                    }
                }
                catch
                {
                    continue;
                }
                
            }
            if (at_least_one_update)
            {
                PlayniteApi.Dialogs.ShowMessage(update_info);
                if (plugin_updated)
                {
                    PrepPluginUpdate(plugin_update_path, plugin_path);
                    PlayniteApi.Dialogs.ShowMessage("VXApp4Playnite Plugin has been updated, please restart Playnite to finish update.");
                }
                settings.EndEdit();
            }
            else
            {
                PlayniteApi.Dialogs.ShowMessage("Tools are Already Up-To-Date!");
            }
            
        }


    }
}
