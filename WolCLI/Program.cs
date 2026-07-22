using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Cryptography;
using System.IO;
using System.Text;

using System.Text.Json.Serialization;

public class WolConfig
{
    [JsonPropertyName("macAddress")] //name inside JSON
    public string ?MacAddress { get; set; }//code name for the property
    
    [JsonPropertyName("passwordHash")]
    public string ?PHash { get; set; }

    [JsonPropertyName("broadcastAddress")]
    public string ?BroadcastAddress { get; set; }
}

class Program
{
    static string configPath = "prod.json";

    static WolConfig ReadConfig()
    {
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<WolConfig>(json) ?? new WolConfig { MacAddress = "", PHash = "", BroadcastAddress = "" };
        }
        return new WolConfig { MacAddress = "", PHash = "", BroadcastAddress = "" };
    }

    static void WriteConfig(WolConfig config)
    {
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    static async Task Main(string[] args)
    {
        await ReadJSON();
        await MainMenuME();
    }

    static async Task MainMenuME()
    {
        while (true)
        {
            await MenuEngine.DisplayMenuAsync(new List<(string, Func<Task>)>
            {
                ("Wake the PC", WakePC),
                ("⚙️ Settings", SettingsMenuME),
                ("❌ Exit", ExitApp)
            });
        }
    }

    static async Task ReadJSON()
    {
        var config = ReadConfig();
        
        if (!string.IsNullOrEmpty(config.MacAddress))
        {
            MenuEngine.GeneralMessage($"MAC address loaded: {config.MacAddress}");
        }
        else
        {
            MenuEngine.ErrorMessage("MAC address not found in JSON.");
            bool confirm = MenuEngine.YesNoPrompt("Would you like to enter a MAC address now?",
                "[green]MAC address entered[/]",
                "[red]Please enter a MAC address[/]");
            if (confirm)
            {
                await EnterMac();
            }
        }
    }
    static async Task EnterBroadcastAddress()
    {
        string broadcastAddress = MenuEngine.TextInput("Enter broadcast address");
        bool confirm = MenuEngine.YesNoPrompt("Is this correct?",
            $"[green]Broadcast address {broadcastAddress} confirmed[/]",
            "[red]Please re-enter the broadcast address[/]");

        if (confirm)
        {
            var config = ReadConfig();
            
            config.BroadcastAddress = broadcastAddress;
            WriteConfig(config);
            MenuEngine.GeneralMessage($"Broadcast address entered: {broadcastAddress}");
        }
        else
        {
            await EnterBroadcastAddress();
        }
    }
    
    static async Task EnterMac()
    {
        string macAddress = MenuEngine.TextInput("Enter MAC address");
        bool confirm = MenuEngine.YesNoPrompt("Is this correct?",
            $"[green]MAC address {macAddress} confirmed[/]",
            "[red]Please re-enter the MAC address[/]");

        if (confirm)
        {
            var config = ReadConfig();
            config.MacAddress = macAddress;
            WriteConfig(config);
            MenuEngine.GeneralMessage($"MAC address entered: {macAddress}");
        }
        else
        {
            await EnterMac();
        }
    }

    static async Task EnterPassword()
    {
        string password = MenuEngine.TextInput("Enter password");
        bool confirm = MenuEngine.YesNoPrompt("Is this correct?",
            "[green]Password confirmed[/]",
            "[red]Please re-enter the password[/]");

        if (!confirm)
        {
            await EnterPassword();
            return;
        }

        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            string hash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            
            var config = ReadConfig();
            config.PHash = hash;
            WriteConfig(config);
            
            MenuEngine.GeneralMessage($"Password hash stored");
        }
    }


    static async Task WakePC()
    {
        
    }

    static async Task SettingsMenuME()
    {
        await MenuEngine.DisplayMenuAsync(new List<(string, Func<Task>)>
        {
            ("🌐 MAC Address", EnterMac),
            ("🔒 Password", EnterPassword),
            ("📡 Broadcast Address", EnterBroadcastAddress),
            ("⬅️ Back", async () => { throw new GoBackException(); })
        });
    }

    
    static async Task ExitApp()
    {
        bool confirm = MenuEngine.YesNoPrompt(
            "Are you sure you want to exit?",
            "[green]Goodbye![/]",
            "[yellow]Returning to main menu[/]"
        );

        if (confirm)
        {
            Environment.Exit(0);
        }
    }
}