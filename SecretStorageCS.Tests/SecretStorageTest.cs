namespace SecretStorageCS.Tests;

public class UnitTest1Test
{
    public async Task<SecretStorage> connectAndGet(string appFolder = "TestApplication")
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

        await storage.ConnectAsync(appFolder);
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

        await storage.CreateItemAsync("TestItem", secretValue, true);
        Assert.Equal(secretValue, await storage.GetItemAsync("TestItem"));
    }

    [Fact]
    public async Task StorageReplace()
    {
        var storage = await connectAndGet();
        byte[] initialValue = System.Text.Encoding.UTF8.GetBytes("InitialValue");
        byte[] newValue = System.Text.Encoding.UTF8.GetBytes("NewValue");

        await storage.CreateItemAsync("TestItem", initialValue, true);
        Assert.Equal(initialValue, await storage.GetItemAsync("TestItem"));

        await storage.CreateItemAsync("TestItem", newValue, true);
        Assert.Equal(newValue, await storage.GetItemAsync("TestItem"));
    }

    [Fact]
    public async Task StorageReplaceExcept()
    {
        var storage = await connectAndGet();
        byte[] initialValue = System.Text.Encoding.UTF8.GetBytes("InitialValue");
        byte[] newValue = System.Text.Encoding.UTF8.GetBytes("NewValue");

        await storage.CreateItemAsync("TestItem", initialValue, true);
        Assert.Equal(initialValue, await storage.GetItemAsync("TestItem"));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await storage.CreateItemAsync("TestItem", newValue, false);
        });

        Assert.Equal(initialValue, await storage.GetItemAsync("TestItem"));
    }

    [Fact]
    public async Task StorageAcrossCtonnections()
    {
        byte[] secretValue = System.Text.Encoding.UTF8.GetBytes("TestValue");
        {
            var storage = await connectAndGet();
            await storage.CreateItemAsync("TestItem", secretValue, true);
        }
        {
            var storage = await connectAndGet();
            Assert.Equal(secretValue, await storage.GetItemAsync("TestItem"));
        }
    }

    [Fact]
    public async Task Deletion()
    {
        var storage = await connectAndGet();
        byte[] secretValue = System.Text.Encoding.UTF8.GetBytes("TestValue");

        await storage.CreateItemAsync("TestItem", secretValue, true);
        Assert.Equal(secretValue, await storage.GetItemAsync("TestItem"));

        Assert.True(await storage.DeleteItemAsync("TestItem"));
        Assert.Null(await storage.GetItemAsync("TestItem"));
    }

    [Fact]
    public async Task DeleteNonExisting()
    {
        var storage = await connectAndGet();
        Assert.False(await storage.DeleteItemAsync("NonExisting"));
    }

    private async Task clearAllData(SecretStorage storage) {
        var keys = await storage.ListItemKeysAsync();
        foreach (var key in keys)
        {
            await storage.DeleteItemAsync(key);
        }
    }

    [Fact]
    public async Task ListItems()
    {
        var storage = await connectAndGet("list1");
        await clearAllData(storage);

        var keys = await storage.ListItemKeysAsync();
        Assert.Empty(keys);

        byte[] secretValue = System.Text.Encoding.UTF8.GetBytes("TestValue");

        await storage.CreateItemAsync("TestItem", secretValue, true);
        await storage.CreateItemAsync("TestItem2", secretValue, true);

        keys = await storage.ListItemKeysAsync();
        Assert.Equal(2, keys.Count);
        Assert.Contains("TestItem", keys);
        Assert.Contains("TestItem2", keys);
    }
}
