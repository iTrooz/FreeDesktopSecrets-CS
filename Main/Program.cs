using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

[SupportedOSPlatform("linux")]
class Program
{
    static async Task Main(string[] args)
    {
        var storage = FreeDesktopSecretsClient.FromSession(); // use the classic dbus session bus
        storage.Logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace)).CreateLogger("Program");
        await storage.ConnectAsync("TestApplication"); // Use this name for the API "folder" that will hold secrets

        var itemKeys = await storage.ListItemKeysAsync(); // List keys of all secrets stored
        foreach (var item in itemKeys)
            Console.WriteLine(item);
        
        await storage.CreateItemAsync("TestItem", System.Text.Encoding.UTF8.GetBytes("TestString"), true); // Store a secret
        var secret = await storage.GetItemAsync("TestItem"); // Retrieve it
        Console.WriteLine(System.Text.Encoding.UTF8.GetString(secret!)); // "TestString"

        await storage.DeleteItemAsync("TestItem"); // Delete the secret
    }
}
