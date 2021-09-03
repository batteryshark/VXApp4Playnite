using System;
using System.IO;
using System.Security.Principal;
using VXShared;

namespace VXLauncher
{
    static class Program
    {
        static App app;
        static WatchDog watchdog;

        public static bool Confirm(string title)
        {
            ConsoleKey response;
            do
            {
                Console.Write($"{ title } [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }

        public static Boolean ConfirmSaveDelete(String path_to_app)
        {
            // Initializes the variables to pass to the MessageBox.Show method.
            return Confirm("Do you Really want to Clear the Saved Data (cache) for " + Path.GetFileName(path_to_app).Replace(".vxapp", "") + " ? The process is irreversible.");
        }

        static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        [STAThread]
        static void Main()
        {
            if (!IsElevated)
            {
                Console.WriteLine("This must be executed with Administrative Privileges.");
                return;
            }
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: vxlauncher.exe \"Path/To/Dir.vxapp\" [cmd=STARTUP/CLEANUP/CLEARCACHE] [config=\"OTHER CONFIG\"] [opt=NOWATCHDOG,NOLAUNCH]");
                return;
            }

            String path_to_app = args[1];
            String operation = "STARTUP";
            String selected_config = "";
            Boolean no_watchdog = false;
            Boolean no_launch = false;



            foreach (String arg in args)
            {
                if (arg.StartsWith("cmd="))
                {
                    operation = arg.Replace("cmd=", "").ToUpper();
                }
                if (arg.StartsWith("config="))
                {
                    selected_config = arg.Replace("config=", "");
                }
                if (arg.StartsWith("opt="))
                {
                    if (arg.Contains("NOWATCHDOG"))
                    {
                        no_watchdog = true;
                    }
                    if (arg.Contains("NOLAUNCH"))
                    {
                        no_launch = true;
                    }

                }
            }
            // Create App Object
            app = new App(path_to_app);
            // Short-Circuit if We're Only Here to Cleanup
            switch (operation)
            {
                case "CLEANUP":
                    app.Unload();
                    Environment.Exit(0);
                    return;
                case "CLEARCACHE":
                    if (ConfirmSaveDelete(app.path))
                    {
                        app.ClearCache();
                    }
                    Environment.Exit(0);
                    return;
            }


            // Otherwise - Let's Get this Party Started
            if (!app.Load(selected_config))
            {
                Console.WriteLine("VXApp Failed to Load", "Error");
                Environment.Exit(0);
                return;
            }

            if (!no_launch)
            {
                if (!app.Launch())
                {
                    Console.WriteLine("VXApp Failed to Launch", "Error");
                    app.Unload();
                    Environment.Exit(0);
                    return;
                }
            }

            if (no_watchdog)
            {
                Console.WriteLine("Press OK When Finished");
                Console.ReadLine();
            }
            else
            {
                watchdog = new WatchDog(app.pipe_name);
                while (watchdog.is_running) { }
            }
            if (app != null)
            {
                app.Unload();
            }
            Environment.Exit(0);
        }
    }
}
