using System.Diagnostics;

public static class WolPackage
{
     public static void SendMagicPacket(string macAddress, string broadcastAddress)
    {
        
        string cleanMac = macAddress.Replace(":", ""); //remove colons
        
        
        string Fcommand = $"printf \"\\xff\\xff\\xff\\xff\\xff\\xff$(printf \"$(echo {cleanMac} | sed 's/../\\\\x&/g')%.0s\" {{1..16}})\" | socat - UDP-DATAGRAM:{broadcastAddress}:9,broadcast"; //command build
        
        
        CommandRunner.RunCommand(Fcommand); //Fcommand = Final command
    }
    

}