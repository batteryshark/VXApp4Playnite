using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;

namespace VXShared
{
    public static class CommandClient
    {
        public static String[] SupportedCommands = { "SHUTDOWN", "SUSPEND", "RESUME" };

        public static Boolean SendCommand(String pipe_name, String Cmd)
        {
            Cmd = Cmd.ToUpper();
            if (!SupportedCommands.Contains(Cmd))
            {
                Console.WriteLine("Invalid Command - Supported Commands: [" + String.Join("/", VXShared.CommandClient.SupportedCommands) + "]");
                return false;
            }
            try
            {
                NamedPipeClientStream npcs = new NamedPipeClientStream(".", pipe_name, PipeDirection.Out);
                StreamWriter sw = new StreamWriter(npcs);
                npcs.Connect(1000);
                sw.Write(Cmd);
                sw.Flush();
                sw.Close();
            }
            catch
            {

                return false;
            }
            return true;
        }
    }
}
