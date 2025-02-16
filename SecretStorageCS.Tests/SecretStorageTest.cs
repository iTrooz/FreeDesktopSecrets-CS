namespace SecretStorageCS.Tests;

public class UnitTest1Test
{
    public async Task<SecretStorage> connectAndGet() {
        var useRealDbus = Environment.GetEnvironmentVariable("USE_REAL_DBUS");
        SecretStorage storage;
        // Make it the default, because I don't want users testing with their real secrets API by mistake
        if (string.IsNullOrEmpty(useRealDbus))
        {
            Console.WriteLine("Using container dbus");
            Console.WriteLine("Current working directory: " + Directory.GetCurrentDirectory());
            storage = await SecretStorage.FromSocket("tcp:host=localhost,port=7834");
        } else {
            Console.WriteLine("Using real dbus");
            storage = await SecretStorage.FromSession();
        }

        await storage.Connect("TestApplication");
        return storage;
    }

    [Fact]
    public async Task Connectivity()
    {
        await connectAndGet();
    }

    [Fact]
    public async Task Storage()
    {
        var storage = await connectAndGet();
        byte[] secretValue = System.Text.Encoding.UTF8.GetBytes("TestValue");

        await storage.CreateItem("TestItem", secretValue, true);
        Assert.Equal(secretValue, await storage.GetItem("TestItem"));
    }
}
