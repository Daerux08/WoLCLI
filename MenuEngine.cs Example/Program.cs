using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        await MainMenuME();
    }
    static async Task MainMenuME()
    {
        while (true) 
        {
           await MenuEngine.DisplayMenuAsync(new List<(string, Func<Task>)>
        {
            ("🎮 Play Game", PlayGameMenuME), //String part is what is shown to the user, action is what executes
            ("⚙️ Settings", SettingsMenuME),//example, this says Settings, and goes to the SettingsMenuME function when selected
            ("ℹ️ About", ShowAbout),
            ("❌ Exit", ExitApp)
        });
        }
        //do the while loop so that the menu is an anchor
        //(AKA, if a menu is exited, it will return to the main menu)
    }
    



    static async Task PlayGameMenuME()
    {
        await MenuEngine.DisplayMenuAsync(new List<(string, Func<Task>)>
        {//The List string and func allows you to expand or shrink the menu,
        //and cuz its relative numbered, you dont have to change IDs
            ("🆚 Play vs AI", PlayVsAI),
            ("👥 Play vs Friend", PlayVsFriend),
            ("⬅️ Back", async () => { throw new GoBackException(); })
        });
    }


    static async Task PlayVsAI()
    {
        MenuEngine.ShowMessage("Game Start", "Starting game vs AI...", true);
        
        var difficulty = MenuEngine.ShowQuickSelectMenu(
            "Select difficulty:",
            new Dictionary<string, int>
            {
                { "Easy", 1 },
                { "Medium", 2 },
                { "Hard", 3 }
            });

        MenuEngine.GeneralMessage($"Playing on difficulty level {difficulty + 1}");
        MenuEngine.GeneralMessage("Simulating game...");
        await Task.Delay(2000);

        bool won = MenuEngine.YesNoPrompt(
            "Did you win?",
            "Great job! Victory recorded.",
            "Better luck next time!"
        );

        MenuEngine.GeneralMessage("\nPress any key to continue...");
        MenuEngine.YesNoPrompt("Do you want to play again?", "Starting a new game...", "Returning to main menu...");
        Console.ReadKey();
    }

    
    
    
    
    
    static async Task PlayVsFriend()
    {
        string player1 = MenuEngine.GetUserInput("Enter Player 1 name", "Player1");
        string player2 = MenuEngine.GetUserInput("Enter Player 2 name", "Player2");

        MenuEngine.ShowMessage("Game Start", $"{player1} vs {player2}", true);
        await Task.Delay(1000);

        string winner = MenuEngine.TextInput("Who won? (Enter player name)");
        MenuEngine.ShowMessage("Game Over", $"🏆 {winner} wins!", false);

        MenuEngine.GeneralMessage("\nPress any key to continue...");
        MenuEngine.YesNoPrompt("Do you want to play again?", "Starting a new game...", "Returning to main menu...");
        Console.ReadKey();
    }


    
    static async Task SettingsMenuME()
    {
        await MenuEngine.DisplayMenuAsync(new List<(string, Func<Task>)>
        {
            ("🔊 Sound: ON", ToggleSound),
            ("🎨 Theme: Dark", ChangeTheme),
            ("👤 Profile", EditProfile),
            ("⬅️ Back", async () => { throw new GoBackException(); })
        });
    }
    

    static async Task ToggleSound()
    {
        bool enabled = MenuEngine.YesNoPrompt(
            "Enable sound?",
            "[green]Sound effects enabled[/]",
            "[yellow]Sound effects disabled[/]"
        );
        MenuEngine.GeneralMessage("\nPress any key to continue...");
        Console.ReadKey();
    }

    static async Task ChangeTheme()
    {
        var theme = MenuEngine.ShowArrowMenu(
            "Select theme:",
            new List<string> { "Dark", "Light", "Ocean", "Forest" });

        MenuEngine.GeneralMessage($"Theme changed to: {new List<string> { "Dark", "Light", "Ocean", "Forest" }[theme]}");
        MenuEngine.GeneralMessage("\nPress any key to continue...");
        Console.ReadKey();
    }

    static async Task EditProfile()
    {
        string username = MenuEngine.GetUserInput("Enter username", "Guest");
        string email = MenuEngine.GetUserInput("Enter email", "user@example.com");

        MenuEngine.ShowMessage("Profile Updated", $"Username: [cyan]{username}[/]\nEmail: [cyan]{email}[/]", true);
        MenuEngine.GeneralMessage("\nPress any key to continue...");
        Console.ReadKey();
    }

    static async Task ShowAbout()
    {
        MenuEngine.ShowMessage("About", 
            "[bold cyan]🎮 Super Game v1.0[/]\n\n" +
            "[yellow]A simple console-based game with multiplayer support.[/]\n\n" +
            "Built with [bold]Spectre.Console[/]\n" +
            "© 2024 Game Studio", 
            true);

        MenuEngine.GeneralMessage("\nPress any key to continue...");
        Console.ReadKey();
    }

    static async Task ExitApp()
    {
        bool confirm = MenuEngine.YesNoPrompt(
            "Are you sure you want to exit?",
            "[green]Goodbye![/]",
            "[yellow]Staying in the game[/]"
        );

        if (confirm)
        {
            AnsiConsole.MarkupLine("[bold green]Thanks for playing! 👋[/]");
            Environment.Exit(0);
        }
    }
}