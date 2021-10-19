using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace V4PUpdater
{
    public class V4PUpdater : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
       


        public override Guid Id { get; } = Guid.Parse("b9e83df4-c46b-4877-9291-ba742c9eb142");

        public static void RecursiveDeletePlugin(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                if (fi.ToString().Contains("V4PUpdater.dll"))
                {
                    continue;
                }
                fi.Delete();
            }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                RecursiveDeletePlugin(di.FullName);
                try
                {
                    di.Delete();
                }
                catch { }

            }
        }

        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                // HACK - Have to Fix this.
                try
                {
                    File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
                }
                catch
                {

                }
            }
        }

        public V4PUpdater(IPlayniteAPI api) : base(api)
        {
            // Resolve Plugin Path
            String plugin_path = Path.Combine(PlayniteApi.Paths.ApplicationPath, "Extensions", "VXApp4Playnite" + "_" + this.Id.ToString());
            // Resolve Update Path
            String update_path = Path.Combine(PlayniteApi.Paths.ApplicationPath, "v4p", "update");
            // Wipe Plugin Path with everything but updater.
            RecursiveDeletePlugin(plugin_path);
            Directory.CreateDirectory(plugin_path);
            // Copy update to plugin path
            CopyFilesRecursively(update_path, plugin_path);
            Directory.Delete(update_path, true);
            PlayniteApi.Dialogs.ShowMessage("[VXApp4Playnite] Update Complete - Please Restart Playnite!");
            Environment.Exit(0);
        }

     


    }
}