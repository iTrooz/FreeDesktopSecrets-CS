using SecretStorageCS;
using System.ComponentModel;
using System.Text;
using Tmds.DBus;

public class SecretStorage
{

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

  #pragma warning disable CS8618
  private Connection Connection { get; set; }
  private IService ServiceProxy { get; set; }
  private ICollection CollectionProxy { get; set; }
  private ObjectPath Session { get; set; }
  private string AppFolder { get; set; }
  #pragma warning restore CS8618

  public async Task Connect(string appFolder)
  {
    AppFolder = appFolder;
    Connection = new Connection(Address.Session);
    await Connection.ConnectAsync();
    Console.WriteLine("Connected !");

    // Create proxies to call methods
    ServiceProxy = Connection.CreateProxy<IService>("org.freedesktop.secrets", "/org/freedesktop/secrets");
    CollectionProxy = Connection.CreateProxy<ICollection>("org.freedesktop.secrets", DEFAULT_COLLECTION);

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
      var watch = await promptProxy.WatchCompletedAsync((result) => { Console.WriteLine($"Prompt completed: {result}"); });
      await promptProxy.PromptAsync("");
    }

    Console.WriteLine($"Unlocked [{string.Join(", ", unlocked)}] ({unlocked.Length}). Prompt: {unlockPrompt}");
  }

  private Dictionary<string, string> getAttributes(string key)
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

  public async Task CreateItem(string key, byte[] value, bool replace)
  {
    var secret = new SecretStruct
    {
      Session = Session,
      Parameters = Encoding.UTF8.GetBytes(""),
      Value = value,
      ContentType = "application/octet-stream"
    };

    if (!replace) {
      // Check if already existing
      var items = await CollectionProxy.SearchItemsAsync(getAttributes(key));
      if (items.Length > 0)
      {
        throw new Exception($"Item with key '{key}' in folder '{AppFolder}' already exists. set `replace` to true to replace it.");
      }
    }

    var (createdItem, prompt) = await CollectionProxy.CreateItemAsync(
      new Dictionary<string, object>
      {
        ["application"] = "MyApp/my-app",
        ["service"] = "MyApp",
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

  public async Task<byte[]> GetItem(string key)
  {
    var items = await CollectionProxy.SearchItemsAsync(getAttributes(key));
    if (items.Length == 0)
    {
      throw new Exception($"Item with key '{key}' in folder '{AppFolder}' not found.");
    }

    var itemProxy = Connection.CreateProxy<IItem>("org.freedesktop.secrets", items[0]);
    SecretStruct secret = new SecretStruct(await itemProxy.GetSecretAsync(Session));
    return secret.Value;
  }

  public async Task<List<string>> ListItemKeys()
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
}

class Program
{
  static async Task Main()
  {
    var secretStorage = new SecretStorage();
    await secretStorage.Connect("MySuperApp");

    await secretStorage.CreateItem("key1", Encoding.UTF8.GetBytes("value1"), true);
    await secretStorage.CreateItem("key2", Encoding.UTF8.GetBytes("value2"), true);

    Console.WriteLine("Secret value of key1: " + Encoding.UTF8.GetString(await secretStorage.GetItem("key1")));

    var itemKeys = await secretStorage.ListItemKeys();
    Console.WriteLine($"List of items ({itemKeys.Count}): {string.Join(", ", itemKeys)}");
  }
}