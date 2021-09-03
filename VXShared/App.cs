using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VXShared
{
    public class App
    {
        public String path;
        String id;
        public Boolean loaded;
        public String pipe_name;
        String name;
        String content_path;
        String local_plugins_path;
        String path_app;
        String path_bin;
        String path_tmp;
        String path_save;
        public String status;
        AppConfig config;

        public String GetBinPath()
        {
            return path_bin;
        }
        public String GetTmpPath()
        {
            return Path.Combine(path_tmp, this.id);
        }
        public String GetTmpRoot()
        {
            return path_tmp;
        }
        public String GetSavePath()
        {
            return Path.Combine(path_save, this.id);
        }

        public String GetSaveRoot()
        {
            return path_save;
        }

        private String ResolvePreloadPath(String preload_name)
        {
            if (preload_name.EndsWith(".dlldynamic"))
            {
                String path_32 = ResolvePreloadPath(preload_name.Replace(".dlldynamic", "32.dll"));
                String path_64 = ResolvePreloadPath(preload_name.Replace(".dlldynamic", "64.dll"));
                if(path_32 != "")
                {
                    return path_32.Replace("32.dll", ".dlldynamic");
                }
                if (path_64 != "")
                {
                    return path_64.Replace("64.dll", ".dlldynamic");
                }
                return "";
            }
            String local_path = Path.Combine(local_plugins_path, preload_name);
            String global_path = Path.Combine(GetBinPath(), preload_name);
            if (File.Exists(local_path)) { return local_path; }
            if (File.Exists(global_path)) { return global_path; }
            return "";
        }

        public Boolean ClearCache()
        { 
            this.status = "Clearing Cache...";
            Directory.Delete(GetSavePath(), true);
            this.status = "Clearing Cache OK!";
            return true;
        }
        public Boolean Load(String selected_config) {
            this.status = "Loading App...";
            // Parse Config
            config = new AppConfig(this.path, selected_config);
            if (!config.valid) {
                this.status = "[VXLauncher] Error: Config Invalid!";
                return false; 
            }
            // Set up Content and Plugins before Smoothie.
            this.content_path = Path.Combine(this.path, "content");
            this.local_plugins_path = Path.Combine(this.path, "plugins");
            this.name = Path.GetFileName(this.path).Replace(".vxapp", "");

            // Setup our Smoothie Instance
            this.status = "Loading App Map...";
            Directory.CreateDirectory(GetTmpPath());
            Directory.CreateDirectory(GetSavePath());
            String mapinfo_path = Path.Combine(content_path, config.map);
            if (!Smoothie.Create(mapinfo_path, GetTmpPath(), GetSavePath()))
            {
                Console.WriteLine("[VXLauncher] Smoothie Create Failed.");
                Directory.Delete(GetTmpPath(), true);
                return false;
            }
            this.status = "Loading App OK!";
            this.loaded = true;
            return true; 
        }
        public Boolean Unload() {
            this.status = "Unloading App...";
            // We'll send a kill signal to the watchdog... just in case.
            if (Directory.Exists(GetTmpPath()))
            {
                CommandClient.SendCommand(this.pipe_name, "SHUTDOWN");
                if (Smoothie.Destroy(GetTmpPath()))
                {
                    Directory.Delete(GetTmpPath(), true);
                }
            }
            
            this.loaded = false;
            this.status = "Unloading App OK!";
            return true;
        }
        public Boolean Launch() {
            this.status = "Launching App...";
            string exe_cpath = "";
            if (!Smoothie.Resolve(GetTmpPath(), config.executable, out exe_cpath))
            {
                this.status = "[VXLauncher] Error: Could not Resolve Target Executable";
                this.Unload();
                return false;
            }

            string resolved_cwd = "";
            if (config.cwd != "")
            {
                if (!Smoothie.Resolve(GetTmpPath(), config.cwd, out resolved_cwd))
                {
                    this.status = "[VXLauncher] Error: Could not Resolve Target CWD";
                    this.Unload();
                    return false;
                }
                resolved_cwd = "\"" + resolved_cwd + "\"";
            }


            if (!File.Exists(exe_cpath))
            {
                this.status = "[VXLauncher] Error: Target Executable Does not Exist";
                this.Unload();
                return false;
            }

            int target_arch = Utils.DetectArch(exe_cpath);

            // Add Application Environment Variables
            foreach (String env in config.envar)
            {
                string[] ev = env.Split('=');
                Environment.SetEnvironmentVariable(ev[0], ev[1]);
            }

            // Add PDXProc Envars
            Environment.SetEnvironmentVariable("PDXPROC", GetBinPath());

            // Add VX Envars
            Environment.SetEnvironmentVariable("VXAPP_ID", this.id);

            // Add PDXFS Libs
            String pdxfs_lib = ResolvePreloadPath("pdxfs.dlldynamic");

            String pdxpl = pdxfs_lib;
            foreach (String preload in config.preload)
            {
                pdxpl += ";";
                pdxpl += ResolvePreloadPath(preload);
            }

            Environment.SetEnvironmentVariable("PDXPL", pdxpl);

            // Add PDXFS Envars
            Environment.SetEnvironmentVariable("PDXFS_ROOT", Path.Combine(GetTmpPath(), "map"));
            Environment.SetEnvironmentVariable("PDXFS_MODE", "1");
            String pdxfs_ignore = GetSaveRoot() + ";" + GetBinPath() + ";" + GetTmpRoot();
            // We're Gonna get rid of some shader directories as well...
            pdxfs_ignore += ";C:\\ProgramData\\Intel;C:\\ProgramData\\Nvidia Corporation;C:\\Users\\USER\\AppData\\Local\\nvidia\\glcache;C:\\Users\\USER\\AppData\\Local\\d3dscache";
            Environment.SetEnvironmentVariable("PDXFS_IGNORE", pdxfs_ignore);
            String dropkick_name = (target_arch == 64) ? "dropkick64.exe" : "dropkick32.exe";
            String pdxproc_name = (target_arch == 64) ? "pdxproc64.dll" : "pdxproc32.dll";
            String dropkick_path = ResolvePreloadPath(dropkick_name);
            String pdxproc_path = ResolvePreloadPath(pdxproc_name);


            ProcessStartInfo startInfo = new ProcessStartInfo(dropkick_path);
            String[] start_args = { "start", "\"" + pdxproc_path + "\"", "\"" + exe_cpath + "\"", resolved_cwd, config.args };
            startInfo.Arguments = String.Join(" ", start_args);
            startInfo.CreateNoWindow = true;
            Console.WriteLine("Launching: " + this.name + " [" + this.id + "]");
            Console.WriteLine("CMD: " + startInfo.Arguments);


            Process.Start(startInfo);
            this.status = "";

            return true;
        }

        public App(String path_to_app)
        {
            this.path = path_to_app;
            
            this.pipe_name = Utils.GetPipeName(path_to_app);
            this.loaded = false;
            id = Utils.DeriveAppCode(this.path);
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string strWorkPath = System.IO.Path.GetDirectoryName(strExeFilePath);
            
            this.path_bin = Utils.GetEnvarWithDefault("VXPATH_BIN", strWorkPath);
            this.path_tmp = Utils.GetEnvarWithDefault("VXPATH_TMP", "C:\\vxtmp");
            this.path_save = Utils.GetEnvarWithDefault("VXPATH_SAVE", "C:\\vxsave");
            this.path_app = Utils.GetEnvarWithDefault("VXPATH_APP", "C:\\apps");

            Directory.CreateDirectory(this.path_app);
            Directory.CreateDirectory(this.path_tmp);
            Directory.CreateDirectory(this.path_save);

        }


    }
}
