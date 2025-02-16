
# SecretStorage-CS

Client library in C# to use the Freedesktop Secret Service API

# Usage

Sample usage of this library:
```csharp
var storage = SecretStorage.FromSession(); // use the classic dbus session bus
await storage.ConnectAsync("TestApplication"); // Use this name for the API "folder" that will hold secrets

var itemKeys = await storage.ListItemKeysAsync(); // List keys of all secrets stored
foreach (var item in itemKeys)
    Console.WriteLine(item);

await storage.CreateItemAsync("TestItem", System.Text.Encoding.UTF8.GetBytes("TestString"), true); // Store a secret
var secret = await storage.GetItemAsync("TestItem"); // Retrieve it
Console.WriteLine(System.Text.Encoding.UTF8.GetString(secret!)); // "TestString"

await storage.DeleteItemAsync("TestItem"); // Delete the secret
```

# Links
- Thanks to tmds for his [dbus library](https://github.com/tmds/Tmds.DBus)
- Inspiration: https://github.com/mitya57/secretstorage
- API Specification: https://specifications.freedesktop.org/- secret-service-spec/latest-single
