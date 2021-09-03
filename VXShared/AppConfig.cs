using System;
using System.IO;
using Newtonsoft.Json;

namespace VXShared
{
    class AppConfig
    {
        public String name;
        public String map;
        public String executable;
        public String args;
        public String cwd;
        public String[] envar;
        public String[] preload;
        public Boolean valid;

        void PrintInfo()
        {
            Console.WriteLine("---------");
            Console.WriteLine("Config Info: ");
            Console.WriteLine("Name: " + this.name);
            Console.WriteLine("Map File: " + this.map);
            Console.WriteLine("Executable: " + this.executable);
            Console.WriteLine("Args: " + this.args);
            Console.WriteLine("Cwd: " + this.cwd);
            Console.WriteLine("Envars: ");
            foreach (String ev in this.envar)
            {
                Console.WriteLine(ev);
            }

            Console.WriteLine("Preload: ");
            foreach (String pl in this.preload)
            {
                Console.WriteLine(pl);
            }
        }
        public AppConfig(String path_to_app, String selected_config)
        {
            valid = false;
            String path_to_vxconfig_file = Path.Combine(path_to_app, "vxapp.config");
            if (!File.Exists(path_to_vxconfig_file)) { return; }
            dynamic config_entries = JsonConvert.DeserializeObject(File.ReadAllText(path_to_vxconfig_file));

            foreach (var entry in config_entries)
            {
                // Hack to support blank config selections (first entry).
                if (selected_config == "")
                {
                    selected_config = entry.name;
                }
                if (entry.name == selected_config)
                {
                    this.name = entry.name;
                    this.map = entry.map;
                    this.executable = entry.executable;
                    this.args = entry.args;
                    this.cwd = entry.cwd;
                    this.envar = entry.envar.ToObject<string[]>();
                    this.preload = entry.preload.ToObject<string[]>();
                }
            }
            if (this.name == "") { Console.WriteLine("Error - Config Not Found."); return; }
            this.valid = true;
            return;
        }
    }
}
