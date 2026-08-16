using System.Diagnostics;

public static class CommandRunner
{
    public static void RunCommand(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            UseShellExecute = false,
        };

        // Pass the -c and the command as separate arguments to avoid shell quoting issues
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(command);

        Process.Start(psi);
    }
}