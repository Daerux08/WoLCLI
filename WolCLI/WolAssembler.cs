using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

public static class WolPackage
{
    /// <summary>
    /// Sends a Wake-on-LAN Magic Packet to wake up a device.
    /// </summary>
    /// <param name="macAddress">MAC address of the target device (format: AA:BB:CC:DD:EE:FF or AABBCCDDEEFF)</param>
    /// <param name="broadcastAddress">Broadcast address (e.g., "255.255.255.255" or your subnet broadcast). If null/empty, uses 255.255.255.255</param>
    public static void SendMagicPacket(string macAddress, string broadcastAddress = null)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            throw new ArgumentException("MAC address is required", nameof(macAddress));

        // Remove any non-hex characters from the MAC address (handles :, -, spaces, etc.)
        string hex = Regex.Replace(macAddress, "[^0-9A-Fa-f]", "");
        if (hex.Length != 12)
            throw new ArgumentException("MAC address must contain 12 hex digits", nameof(macAddress));

        // Convert hex string to byte array
        byte[] mac = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            mac[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        // Build magic packet: 6 x 0xFF followed by MAC repeated 16 times
        byte[] packet = new byte[6 + 16 * mac.Length];
        for (int i = 0; i < 6; i++)
            packet[i] = 0xFF;
        for (int i = 0; i < 16; i++)
            Array.Copy(mac, 0, packet, 6 + i * mac.Length, mac.Length);

        // Determine broadcast address
        IPAddress ip;
        if (string.IsNullOrWhiteSpace(broadcastAddress))
        {
            ip = IPAddress.Broadcast; // 255.255.255.255
        }
        else
        {
            ip = IPAddress.Parse(broadcastAddress);
        }

        // Send the packet via UDP
        using (var client = new UdpClient())
        {
            client.EnableBroadcast = true;
            client.Send(packet, packet.Length, new IPEndPoint(ip, 9));
        }
    }
}