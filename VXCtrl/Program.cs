using System;

namespace VXCtrl
{
    class VXCtrl
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: VXCtrl.exe path/to/target.vxapp [" + String.Join("/", VXShared.CommandClient.SupportedCommands) + "]");
                return;
            }

            String pipe_name = VXShared.Utils.GetPipeName(args[0]);
            Boolean status = VXShared.CommandClient.SendCommand(pipe_name, args[1]);
            Console.WriteLine("SendCommand: " + status);
        }
    }
}
