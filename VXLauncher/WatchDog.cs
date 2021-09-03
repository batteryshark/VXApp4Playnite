using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace VXLauncher
{
    public class WatchDog
    {
        public volatile Boolean is_running;
        String server_name;
        Thread listener_thread;
        Thread watchdog_thread;
        NamedPipeServerStream pipeServer;

        void WatchdogThread(object pm)
        {
            ProcessManager vpm = (ProcessManager)pm;
            while (is_running)
            {
                if (vpm.initialized)
                {
                    if (!vpm.is_running)
                    {
                        this.Stop();
                    }
                }
            }
        }

        void CommandListenerThread(object pm)
        {
            // Spawn Process Manager
            ProcessManager vpm = (ProcessManager)pm;
            while (is_running)
            {
                try
                {

                    pipeServer = new NamedPipeServerStream(server_name, PipeDirection.In);

                    pipeServer.WaitForConnection();
                    StreamReader sr = new StreamReader(pipeServer);
                    String cmd = sr.ReadToEnd().Replace("\0", string.Empty);
                    //Console.WriteLine("Command " + cmd);
                    sr.Close();
                    switch (cmd)
                    {
                        case "SHUTDOWN":
                            vpm.ShutdownApp();
                            Stop();
                            break;
                        case "SUSPEND":
                            vpm.SuspendApp();
                            break;
                        case "RESUME":
                            vpm.ResumeApp();
                            break;
                        default:
                            break;
                    }
                    if (cmd.StartsWith("REGISTER "))
                    {
                        cmd = cmd.Replace("REGISTER ", "");
                        vpm.Register(int.Parse(cmd));
                    }
                }
                catch { continue; }

            }
        }

        public void Stop()
        {
            this.is_running = false;
            watchdog_thread.Abort();
            pipeServer.Close();
            listener_thread.Abort();


        }

        public void Start(ProcessManager pm)
        {
            this.is_running = true;
            listener_thread = new Thread(this.CommandListenerThread);
            listener_thread.Start(pm);
            watchdog_thread = new Thread(this.WatchdogThread);
            watchdog_thread.Start(pm);
        }
        public void SetServerName(String server_name)
        {
            this.server_name = server_name;
        }

        public WatchDog(String server_name)
        {
            this.SetServerName(server_name);
            this.Start(new ProcessManager());
        }
    }
}
