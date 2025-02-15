using SecretStorageCS;
using System.Text;
using Tmds.DBus;

public struct SecretStruct
{
  public ObjectPath Session { get; set; }
  public byte[] Parameters { get; set; }
  public byte[] Value { get; set; }
  public string ContentType { get; set; }

  public static implicit operator (ObjectPath, byte[], byte[], string)(SecretStruct secretStruct)
  {
    return (secretStruct.Session, secretStruct.Parameters, secretStruct.Value, secretStruct.ContentType);
  }
}

public class SecretStorage
{
  public const string DEFAULT_COLLECTION = "/org/freedesktop/secrets/aliases/default";
  private Connection Connection { get; set; }
  private IService ServiceProxy { get; set; }
  private ICollection CollectionProxy { get; set; }
  private ObjectPath Session { get; set; }
  private string AppFolder { get; set; }

  public async Task Init(string appFolder)
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
    var (unlocked, unlockPrompt) = await ServiceProxy.UnlockAsync([DEFAULT_COLLECTION]);
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

  public async Task CreateItem(string key, byte[] value)
  {
    var secret = new SecretStruct
    {
      Session = Session,
      Parameters = Encoding.UTF8.GetBytes(""),
      Value = value,
      ContentType = "text/plain"
    };

    var (createdItem, prompt) = await this.CollectionProxy.CreateItemAsync(
      new Dictionary<string, object>
      {
        ["application"] = "MyApp/my-app",
        ["service"] = "MyApp",
        ["org.freedesktop.Secret.Item.Label"] = AppFolder + "/" + key
      },
      secret, false
    );
    Console.WriteLine($"Secret created (createdItem: {createdItem}, prompt: {prompt})");
  }

  public async Task ListItems()
  {
    var props = await this.CollectionProxy.GetAllAsync();
    foreach (var item in props.Items)
    {
      var itemProxy = this.Connection.CreateProxy<IItem>("org.freedesktop.secrets", item);
      var itemProps = await itemProxy.GetAllAsync();
      Console.WriteLine($"Item: {item}, Type: {itemProps.Type}, Label: {itemProps.Label}");
      Console.WriteLine($"Attributes ({itemProps.Attributes.Count}): {string.Join(", ", itemProps.Attributes)}");
      Console.WriteLine();
    }
  }
  
  // Doesn't work well, only returns some items
  public async Task ListItemsLegacy()
  {
    var res = await this.CollectionProxy.SearchItemsAsync(new Dictionary<string, string>());
    Console.WriteLine($"Listing {res.Length} items: {string.Join(", ", res)}");
    foreach (var item in res)
    {
      // Get item
      var itemProxy = this.Connection.CreateProxy<IItem>("org.freedesktop.secrets", item);
      var props = await itemProxy.GetAllAsync();
      Console.WriteLine("----------");
      Console.WriteLine($"Props: {props.Created}, {props.Modified}, {props.Label}, {props.Type}");
      Console.WriteLine($"Attributes: {string.Join(", ", props.Attributes)}");
    }
  }
}

class Program
{
  static async Task Main()
  {
    var secretStorage = new SecretStorage();
    await secretStorage.Init("MySuperApp");
    await secretStorage.CreateItem("key1", Encoding.UTF8.GetBytes("value1"));
    await secretStorage.ListItems();
  }
}