using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

public class ServerEntry
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("broadcastAddress")]
    public string? BroadcastAddress { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

public class AppConfig
{
    [JsonPropertyName("servers")]
    public List<ServerEntry> Servers { get; set; } = new List<ServerEntry>();

    [JsonPropertyName("passwordHash")]
    public string? PHash { get; set; }
}

// Keep the legacy single-entry type to support migration
public class WolConfig
{
    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("passwordHash")]
    public string? PHash { get; set; }

    [JsonPropertyName("broadcastAddress")]
    public string? BroadcastAddress { get; set; }
}

// ─── Native AOT JSON Source Generator Context ───
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ServerEntry))]
[JsonSerializable(typeof(WolConfig))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

partial class Program
{
    static readonly string configPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WolCLI",
            "prod.json"
        );

    static AppConfig ReadConfig()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);

            // Try to deserialize as new AppConfig first
            try
            {
                var appCfg = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppConfig);
                if (appCfg != null && appCfg.Servers != null && appCfg.Servers.Count > 0)
                    return appCfg;
            }
            catch { }

            // Fallback: try legacy single-entry WolConfig and migrate
            try
            {
                var legacy = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.WolConfig);
                if (legacy != null)
                {
                    var migrated = new AppConfig();
                    migrated.PHash = legacy.PHash;
                    migrated.Servers.Add(new ServerEntry
                    {
                        Name = "default",
                        MacAddress = legacy.MacAddress ?? "",
                        BroadcastAddress = legacy.BroadcastAddress ?? "",
                        Enabled = true
                    });

                    WriteConfig(migrated);
                    MenuEngine.GeneralMessage("Migrated legacy config to servers list.");
                    return migrated;
                }
            }
            catch { }

            // If all else fails, return a default empty config and write it
            var defaultConfig = new AppConfig
            {
                PHash = "",
                Servers = new List<ServerEntry> { new ServerEntry { Name = "default", MacAddress = "", BroadcastAddress = "", Enabled = true } }
            };

            string outJson = JsonSerializer.Serialize(defaultConfig, AppJsonSerializerContext.Default.AppConfig);
            File.WriteAllText(configPath, outJson);
            MenuEngine.GeneralMessage("Config file created or repaired.");
            return defaultConfig;
        }

        // Create directory and default config if file does not exist
        var newConfig = new AppConfig
        {
            PHash = "",
            Servers = new List<ServerEntry> { new ServerEntry { Name = "default", MacAddress = "", BroadcastAddress = "", Enabled = true } }
        };
        string newJson = JsonSerializer.Serialize(newConfig, AppJsonSerializerContext.Default.AppConfig);
        File.WriteAllText(configPath, newJson);
        MenuEngine.GeneralMessage("Config file not found. Creating a new one.");
        return newConfig;
    }

    static void WriteConfig(AppConfig config)
    {
        string json = JsonSerializer.Serialize(config, AppJsonSerializerContext.Default.AppConfig);
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
                ("Wake the PC", PasswordPrompt),
                ("Wake all", WakeAll),
                ("⚙️ Settings", SettingsMenuME),
                ("❌ Exit", ExitApp)
            });
        }
    }

    static async Task PasswordPrompt()
    {
        var cfg = ReadConfig();
        if (string.IsNullOrEmpty(cfg.PHash))
        {
            await WakePC();
        }
        else
        {
            await VerifyPassword();
        }
    }

    static async Task VerifyPassword()
    {
        var config = ReadConfig();
        string password = MenuEngine.TextInput("Enter password");
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            string hash = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

            if (hash == config.PHash)
            {
                await WakePC();
            }
            else
            {
                MenuEngine.ErrorMessage("Incorrect password. Please try again.");
                await PasswordPrompt();
            }
        }
    }

    static async Task ReadJSON()
    {
        var config = ReadConfig();

        if (config.Servers != null && config.Servers.Count > 0 && !string.IsNullOrEmpty(config.Servers[0].MacAddress))
        {
            MenuEngine.GeneralMessage($"Primary MAC address loaded: {config.Servers[0].MacAddress}");
        }
        else
        {
            MenuEngine.ErrorMessage("No MAC address found in config.");
            bool confirm = MenuEngine.YesNoPrompt("Would you like to enter a MAC address now?",
                "[green]Proceeding...[/]",
                "[red]Please enter the MAC address of the target PC[/]");
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
            if (config.Servers == null || config.Servers.Count == 0)
                config.Servers = new List<ServerEntry> { new ServerEntry { Name = "default" } };

            config.Servers[0].BroadcastAddress = broadcastAddress;
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
            if (config.Servers == null || config.Servers.Count == 0)
                config.Servers = new List<ServerEntry> { new ServerEntry { Name = "default" } };

            config.Servers[0].MacAddress = macAddress;
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
        var config = ReadConfig();
        if (config.Servers == null || config.Servers.Count == 0)
        {
            MenuEngine.ErrorMessage("No server configured. Please enter a MAC address first.");
            await EnterMac();
            return;
        }

        var primary = config.Servers[0];
        if (string.IsNullOrEmpty(primary.MacAddress))
        {
            MenuEngine.ErrorMessage("MAC address not found. Please enter a MAC address first.");
            await EnterMac();
            return;
        }

        if (string.IsNullOrEmpty(primary.BroadcastAddress))
        {
            MenuEngine.ErrorMessage("Broadcast address not found. Please enter a broadcast address first.");
            await EnterBroadcastAddress();
            return;
        }

        WolPackage.SendMagicPacket(primary.MacAddress, primary.BroadcastAddress);
        MenuEngine.GeneralMessage($"Sent Wake-on-LAN packet to {primary.MacAddress}");
    }

    static async Task SettingsMenuME()
    {
        await MenuEngine.DisplayMenuAsync(new List<(string, Func<Task>)>
        {
            ("🌐 MAC Address", EnterMac),
            ("🔒 Password", EnterPassword),
            ("📡 Broadcast Address", EnterBroadcastAddress),
            ("🗂️ Manage Servers", ManageServersMenu),
            ("⬅️ Back", async () => { throw new GoBackException(); })
        });
    }

    static async Task ManageServersMenu()
    {
        while (true)
        {
            var cfg = ReadConfig();
            var options = new List<string>();
            if (cfg.Servers != null && cfg.Servers.Count > 0)
            {
                foreach (var s in cfg.Servers)
                {
                    string status = s.Enabled ? "(enabled)" : "(disabled)";
                    options.Add($"{s.Name ?? "unnamed"} {status} - {s.MacAddress}");
                }
            }
            options.Add("+ Add Server");
            options.Add("⬅️ Back");

            int choice = MenuEngine.ShowArrowMenu("Manage Servers", options);

            if (choice < (cfg.Servers?.Count ?? 0))
            {
                await EditServerByIndex(choice);
                continue;
            }

            int adjusted = choice - (cfg.Servers?.Count ?? 0);
            if (options[choice] == "+ Add Server")
            {
                await AddServer();
                continue;
            }

            // Back
            return;
        }
    }

    static async Task AddServer()
    {
        string name = MenuEngine.TextInput("Enter server name");
        string mac = MenuEngine.TextInput("Enter MAC address");
        string broadcast = MenuEngine.TextInput("Enter broadcast address (optional)");

        bool confirm = MenuEngine.YesNoPrompt("Add this server?",
            $"[green]{name} added[/]",
            "[red]Cancelled[/]");

        if (!confirm) return;

        var cfg = ReadConfig();
        cfg.Servers ??= new List<ServerEntry>();
        cfg.Servers.Add(new ServerEntry { Name = name, MacAddress = mac, BroadcastAddress = broadcast, Enabled = true });
        WriteConfig(cfg);
        MenuEngine.GeneralMessage($"Server {name} added.");
    }

    static async Task EditServerByIndex(int idx)
    {
        var cfg = ReadConfig();
        if (cfg.Servers == null || idx < 0 || idx >= cfg.Servers.Count) return;

        var s = cfg.Servers[idx];

        await MenuEngine.DisplayMenuAsync(new List<(string, Func<Task>)>
        {
            ($"✏️ Edit name ({s.Name})", async () => { s.Name = MenuEngine.TextInput("Enter new name"); WriteConfig(cfg); MenuEngine.GeneralMessage("Name updated"); }),
            ($"🖧 Edit MAC ({s.MacAddress})", async () => { s.MacAddress = MenuEngine.TextInput("Enter new MAC"); WriteConfig(cfg); MenuEngine.GeneralMessage("MAC updated"); }),
            ($"📡 Edit broadcast ({s.BroadcastAddress})", async () => { s.BroadcastAddress = MenuEngine.TextInput("Enter new broadcast"); WriteConfig(cfg); MenuEngine.GeneralMessage("Broadcast updated"); }),
            ($"🔁 Toggle enabled ({(s.Enabled?"enabled":"disabled")})", async () => { s.Enabled = !s.Enabled; WriteConfig(cfg); MenuEngine.GeneralMessage($"Enabled={s.Enabled}"); }),
            ($"🗑️ Delete server", async () => {
                bool conf = MenuEngine.YesNoPrompt($"Delete {s.Name ?? s.MacAddress}?","[green]Deleted[/]","[red]Cancelled[/]");
                if (conf) { cfg.Servers.RemoveAt(idx); WriteConfig(cfg); MenuEngine.GeneralMessage("Deleted"); throw new GoBackException(); }
            }),
            ("⬅️ Back", async () => { throw new GoBackException(); })
        });
    }

    static async Task WakeAll()
    {
        var cfg = ReadConfig();
        var servers = cfg.Servers ?? new List<ServerEntry>();
        var enabled = servers.FindAll(s => s.Enabled);
        if (enabled.Count == 0)
        {
            MenuEngine.ErrorMessage("No enabled servers found in config.");
            return;
        }

        var tasks = new List<Task<string>>();
        foreach (var s in enabled)
        {
            if (string.IsNullOrWhiteSpace(s.MacAddress))
            {
                tasks.Add(Task.FromResult($"Skipped {s.Name ?? "unnamed"}: no MAC address"));
                continue;
            }

            string bc = string.IsNullOrWhiteSpace(s.BroadcastAddress) ? null : s.BroadcastAddress;

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    WolPackage.SendMagicPacket(s.MacAddress!, bc);
                    return $"Sent Wake-on-LAN packet to {s.MacAddress} ({s.Name})";
                }
                catch (Exception ex)
                {
                    return $"Failed {s.Name ?? s.MacAddress}: {ex.Message}";
                }
            }));
        }

        var results = await Task.WhenAll(tasks);
        foreach (var r in results)
            MenuEngine.GeneralMessage(r);
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