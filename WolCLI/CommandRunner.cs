using System.Diagnostics;

    public static class CommandRunner
    {
        public static void RunCommand(string command)
        {
            Process.Start("/bin/bash", $"-c \"{command}\"");
        }
    }
   