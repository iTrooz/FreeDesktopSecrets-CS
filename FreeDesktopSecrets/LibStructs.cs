using Tmds.DBus;

[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("Tmds.DBus.Emit")]

namespace iTrooz.FreeDesktopSecrets;

[DBusInterface("org.freedesktop.Secret.Service")]
internal interface IService : IDBusObject
{
  Task<(object output, ObjectPath result)> OpenSessionAsync(string algorithm, object input);
  Task<(ObjectPath[] unlocked, ObjectPath prompt)> UnlockAsync(ObjectPath[] objects);
  Task<T> GetAsync<T>(string prop);
}

[DBusInterface("org.freedesktop.Secret.Collection")]
internal interface ICollection : IDBusObject
{
  Task<(ObjectPath item, ObjectPath prompt)> CreateItemAsync(IDictionary<string, object> properties, (ObjectPath, byte[], byte[], string) secret, bool replace);
  Task<CollectionProperties> GetAllAsync();
  Task<ObjectPath[]> SearchItemsAsync(IDictionary<string, string> Attributes);
}

[Dictionary]
internal class CollectionProperties
{
  private ObjectPath[] _Items = default(ObjectPath[]);
  internal ObjectPath[] Items
  {
    get
    {
      return _Items;
    }

    set
    {
      _Items = (value);
    }
  }

  private string _Label = default(string);
  internal string Label
  {
    get
    {
      return _Label;
    }

    set
    {
      _Label = (value);
    }
  }

  private bool _Locked = default(bool);
  internal bool Locked
  {
    get
    {
      return _Locked;
    }

    set
    {
      _Locked = (value);
    }
  }

  private ulong _Created = default(ulong);
  internal ulong Created
  {
    get
    {
      return _Created;
    }

    set
    {
      _Created = (value);
    }
  }

  private ulong _Modified = default(ulong);
  internal ulong Modified
  {
    get
    {
      return _Modified;
    }

    set
    {
      _Modified = (value);
    }
  }
}


[DBusInterface("org.freedesktop.Secret.Prompt")]
internal interface IPrompt : IDBusObject
{
  Task PromptAsync(String windowID);
  Task<IDisposable> WatchCompletedAsync(Action<(bool dismissed, object result)> handler);
}

[DBusInterface("org.freedesktop.Secret.Item")]
internal interface IItem : IDBusObject
{
  Task<ObjectPath> DeleteAsync();
  Task<(ObjectPath secret, byte[], byte[], string)> GetSecretAsync(ObjectPath Session);
  Task SetSecretAsync((ObjectPath, byte[], byte[], string) Secret);
  Task<T> GetAsync<T>(string prop);
  Task<ItemProperties> GetAllAsync();
  Task SetAsync(string prop, object val);
  Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

[Dictionary]
internal class ItemProperties
{
  private bool _Locked = default(bool);
  internal bool Locked
  {
    get
    {
      return _Locked;
    }

    set
    {
      _Locked = (value);
    }
  }

  private IDictionary<string, string> _Attributes = default(IDictionary<string, string>);
  internal IDictionary<string, string> Attributes
  {
    get
    {
      return _Attributes;
    }

    set
    {
      _Attributes = (value);
    }
  }

  private string _Label = default(string);
  internal string Label
  {
    get
    {
      return _Label;
    }

    set
    {
      _Label = (value);
    }
  }

  private string _Type = default(string);
  internal string Type
  {
    get
    {
      return _Type;
    }

    set
    {
      _Type = (value);
    }
  }

  private ulong _Created = default(ulong);
  internal ulong Created
  {
    get
    {
      return _Created;
    }

    set
    {
      _Created = (value);
    }
  }

  private ulong _Modified = default(ulong);
  internal ulong Modified
  {
    get
    {
      return _Modified;
    }

    set
    {
      _Modified = (value);
    }
  }
}