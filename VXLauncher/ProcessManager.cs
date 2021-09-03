using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace VXLauncher
{
    public class ProcessManager
    {
        public List<int> app_pids = new List<int> { };
        public volatile Boolean is_suspended = false;
        public volatile Boolean is_running = false;
        public volatile Boolean initialized = false;

        Thread refresh_thread;

        public void Register(int pid)
        {
            this.app_pids.Add(pid);
            if (this.app_pids.Count() == 1)
            {
                Start();
            }
        }

        public void Unregister(int pid)
        {
            this.app_pids.Remove(pid);
            if (this.app_pids.Count() == 0)
            {
                Stop();
            }
        }

        public void Refresh()
        {
            while (is_running)
            {
                if (is_suspended) { continue; }
                var allProcesses = Process.GetProcesses();

                List<int> current_pids = new List<int>();
                List<int> pids_to_remove = new List<int>();

                foreach (var proc in allProcesses)
                {
                    current_pids.Add(proc.Id);
                }

                // Check our active list and see if any are dead.
                foreach (int pid in app_pids)
                {

                    if (!current_pids.Contains(pid))
                    {
                        pids_to_remove.Add(pid);
                    }
                }

                foreach (int pid in pids_to_remove)
                {
                    Unregister(pid);
                }
            }
        }

        public void SuspendApp()
        {
            if (is_suspended) { return; }
            foreach (int pid in this.app_pids)
            {
                var process = Process.GetProcessById(pid);
                process.Suspend();
            }
            is_suspended = true;
        }

        public void ResumeApp()
        {
            if (!is_suspended) { return; }
            foreach (int pid in this.app_pids)
            {
                var process = Process.GetProcessById(pid);
                process.Resume();
            }
            is_suspended = false;
        }

        public void ShutdownApp()
        {
            if (!is_running) { return; }
            if (is_suspended) { is_suspended = false; }

            foreach (int p in this.app_pids)
            {
                try
                {
                    Process.GetProcessById(p).Kill();
                }
                catch { }

            }
            this.app_pids.Clear();
            this.Stop();
        }

        public void Stop()
        {
            this.is_running = false;
            this.refresh_thread.Abort();
        }

        public void Start()
        {
            is_running = true;
            refresh_thread = new Thread(this.Refresh);
            refresh_thread.Start();
            initialized = true;
        }
    }
}
