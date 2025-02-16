namespace SecretStorageCS.Tests;

public class UnitTest1Test
{
    public async Task<SecretStorage> connectAndGet()
    {
        var useRealDbus = Environment.GetEnvironmentVariable("USE_REAL_DBUS");
        SecretStorage storage;
        // Make it the default, because I don't want users testing with their real secrets API by mistake
        if (string.IsNullOrEmpty(useRealDbus))
        {
            Console.WriteLine("Using container dbus");
            Console.WriteLine("Current working directory: " + Directory.GetCurrentDirectory());
            storage = SecretStorage.FromSocket("tcp:host=localhost,port=7834");
        }
        else
        {
            Console.WriteLine("Using real dbus");
            storage = SecretStorage.FromSession();
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
    [Fact]
    public async Task StorageAcrossCtonnections()
    {
        byte[] secretValue = System.Text.Encoding.UTF8.GetBytes("TestValue");
        {
            var storage = await connectAndGet();
            await storage.CreateItem("TestItem", secretValue, true);
        }
        {
            var storage = await connectAndGet();
            Assert.Equal(secretValue, await storage.GetItem("TestItem"));
        }
    }

    [Fact]
    public async Task Deletion()
    {
        var storage = await connectAndGet();
        byte[] secretValue = System.Text.Encoding.UTF8.GetBytes("TestValue");

        await storage.CreateItem("TestItem", secretValue, true);
        Assert.Equal(secretValue, await storage.GetItem("TestItem"));

        await storage.DeleteItem("TestItem");
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await storage.GetItem("TestItem"));
    }

    [Fact]
    public async Task DeleteNonExisting()
    {
        var storage = await connectAndGet();
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await storage.DeleteItem("NonExisting"));
    }

    [Fact]
    public async Task ListItems()
    {
        var storage = await connectAndGet();
        byte[] secretValue = System.Text.Encoding.UTF8.GetBytes("TestValue");

        await storage.CreateItem("TestItem", secretValue, true);
        await storage.CreateItem("TestItem2", secretValue, true);

        var keys = await storage.ListItemKeys();
        Assert.Equal(2, keys.Count);
        Assert.Contains("TestItem", keys);
        Assert.Contains("TestItem2", keys);
    }
}
