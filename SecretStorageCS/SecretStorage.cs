#nullable enable
using SecretStorageCS;
using System.Diagnostics.Contracts;
using System.Text;
using Tmds.DBus;

public class SecretStorage
{

    /// <summary>
    ///  Manages secrets for a given folder, in the default secrets collection
    /// </summary>
    struct SecretStruct
    {

        public ObjectPath Session { get; set; }
        public byte[] Parameters { get; set; }
        public byte[] Value { get; set; }
        public string ContentType { get; set; }

        public SecretStruct((ObjectPath, byte[], byte[], string) tuple)
        {
            Session = tuple.Item1;
            Parameters = tuple.Item2;
            Value = tuple.Item3;
            ContentType = tuple.Item4;
        }

        public static implicit operator (ObjectPath, byte[], byte[], string)(SecretStruct secretStruct)
        {
            return (secretStruct.Session, secretStruct.Parameters, secretStruct.Value, secretStruct.ContentType);
        }
    }

    private const string DEFAULT_COLLECTION = "/org/freedesktop/secrets/aliases/default";

    private Connection Connection { get; set; }
    private IService ServiceProxy { get; set; }
    private ICollection CollectionProxy { get; set; }
    private ObjectPath Session { get; set; }
    private string AppFolder { get; set; } = null!;

    private SecretStorage(Connection connection)
    {
        Connection = connection;
        // Create proxies to call methods
        ServiceProxy = Connection.CreateProxy<IService>("org.freedesktop.secrets", "/org/freedesktop/secrets");
        CollectionProxy = Connection.CreateProxy<ICollection>("org.freedesktop.secrets", DEFAULT_COLLECTION);
    }

    [Pure]
    public static SecretStorage FromSession()
    {
        return new SecretStorage(new Connection(Address.Session));
    }

    [Pure]
    public static SecretStorage FromSocket(string socketPath)
    {
        return new SecretStorage(new Connection(socketPath));
    }

    public async Task ConnectAsync(string appFolder)
    {
        AppFolder = appFolder;
        await Connection.ConnectAsync();
        Console.WriteLine("Connected !");

        await CreateSession();
        await UnlockSession();
    }

    private async Task CreateSession()
    {
        Session = (await ServiceProxy.OpenSessionAsync("plain", "my-session")).result;
        Console.WriteLine($"Created session: {Session}");
    }

    private async Task UnlockSession()
    {
        var (unlocked, unlockPrompt) = await ServiceProxy.UnlockAsync(new ObjectPath[] { DEFAULT_COLLECTION });
        if (unlockPrompt == "/")
        {
            Console.WriteLine("No need to prompt for unlocking");
        }
        else
        {
            Console.WriteLine("Unlocking failed. Prompting for unlocking");
            var promptProxy = Connection.CreateProxy<IPrompt>("org.freedesktop.secrets", unlockPrompt);

            // Unlock and wait for unlock to complete
            var tcs = new TaskCompletionSource<bool>();
            var watch = await promptProxy.WatchCompletedAsync((result) =>
            {
                Console.WriteLine($"Unlock prompt completed: {result}");
                tcs.SetResult(true);
            });
            await promptProxy.PromptAsync("");
            await tcs.Task;
        }

        Console.WriteLine($"Unlocked [{string.Join(", ", unlocked)}] ({unlocked.Length}). Prompt: {unlockPrompt}");
    }

    private Dictionary<string, string> getAttributes(string? key)
    {
        var d = new Dictionary<string, string>
    {
      { "appFolder", AppFolder }
    };
        if (key != null)
        {
            d["key"] = key;
        }
        return d;
    }

    /// <summary>
    /// Store a secret value associated to the given key
    /// </summary>
    /// <param name="replace">
    /// Whether to overwrite the value if something is already associated with this key.
    /// If false, an exception will be thrown if the key already exists.
    /// </param>
    public async Task CreateItemAsync(string key, byte[] value, bool replace)
    {
        var secret = new SecretStruct
        {
            Session = Session,
            Parameters = Encoding.UTF8.GetBytes(""),
            Value = value,
            ContentType = "application/octet-stream"
        };

        if (!replace)
        {
            // Check if already existing
            var items = await CollectionProxy.SearchItemsAsync(getAttributes(key));
            if (items.Length > 0)
            {
                throw new InvalidOperationException($"Item with key '{key}' in folder '{AppFolder}' already exists. set `replace` to true to replace it.");
            }
        }

        var (createdItem, prompt) = await CollectionProxy.CreateItemAsync(
          new Dictionary<string, object>
          {
              ["org.freedesktop.Secret.Item.Label"] = AppFolder + "/" + key,
              ["org.freedesktop.Secret.Item.Attributes"] = getAttributes(key),
          },
          secret,
          // Note: The 'replace' Freedesktop Secrets API parameter only works on *attributes*, not all *properties*
          // It will replace if it finds secrets with the same *attributes*
          true
        );
        Console.WriteLine($"Secret created (createdItem: {createdItem}, prompt: {prompt})");
    }

    /// <summary>
    /// Retrieve the secret value associated to a given key
    /// </summary>
    public async Task<byte[]?> GetItemAsync(string key) {
        var items = await CollectionProxy.SearchItemsAsync(getAttributes(key));
        if (items.Length == 0)
        {
            return null;
        }

        var itemProxy = Connection.CreateProxy<IItem>("org.freedesktop.secrets", items[0]);
        SecretStruct secret = new SecretStruct(await itemProxy.GetSecretAsync(Session));
        return secret.Value;
    }

    public async Task<bool> DeleteItemAsync(string key)
    {
        var items = await CollectionProxy.SearchItemsAsync(getAttributes(key));
        if (items.Length == 0)
        {
            return false;
        }
        else if (items.Length > 1)
        {
            Console.WriteLine($"Multiple items with key '{key}' in folder '{AppFolder}' found. This should not happen. Deleting the first one.");
        }

        var itemProxy = Connection.CreateProxy<IItem>("org.freedesktop.secrets", items[0]);
        await itemProxy.DeleteAsync();
        return true;
    }

    /// <summary>
    /// List all keys in your app's folder
    /// </summary>
    public async Task<List<string>> ListItemKeysAsync()
    {
        List<string> keys = new List<string>();
        var items = await CollectionProxy.SearchItemsAsync(getAttributes(null));
        foreach (var item in items)
        {
            var itemProxy = Connection.CreateProxy<IItem>("org.freedesktop.secrets", item);
            var props = await itemProxy.GetAllAsync();
            keys.Add(props.Attributes["key"]);
        }
        return keys;
    }


    public void Connect(string appFolder) => ConnectAsync(appFolder).GetAwaiter().GetResult();

    public void CreateItem(string key, byte[] value, bool replace) => CreateItemAsync(key, value, replace).GetAwaiter().GetResult();

    public byte[]? GetItem(string key) => GetItemAsync(key).GetAwaiter().GetResult();

    public bool DeleteItem(string key) => DeleteItemAsync(key).GetAwaiter().GetResult();

    public List<string> ListItemKeys() => ListItemKeysAsync().GetAwaiter().GetResult();
}
