using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

[SupportedOSPlatform("linux")]
class Program
{
    static async Task Main(string[] args)
    {
        var storage = SecretStorage.FromSession();
        storage.Logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace)).CreateLogger("Program");
        await storage.ConnectAsync("TestApplication");
        foreach (var item in await storage.ListItemKeysAsync())
        {
            Console.WriteLine(item);
        }
    }
}
