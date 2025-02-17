
# FreeDesktopSecrets-CS

Client library in C# to use the Freedesktop Secrets API

# What ?
The Freedesktop Secrets API is a standard for storing secrets (passwords, keys, etc.) in a secure way on a Linux system. It is implemented over dbus, which is a message bus system that allows communication between applications.
This library is a C# client to use this API.

# Usage

Sample usage of this library:
```csharp
var storage = FreeDesktopSecretsClient.FromSession(); // use the classic dbus session bus
await storage.ConnectAsync("TestApplication"); // Use this name for the API "folder" that will hold secrets

var itemKeys = await storage.ListItemKeysAsync(); // List keys of all secrets stored
foreach (var item in itemKeys)
    Console.WriteLine(item);

await storage.CreateItemAsync(
    "TestItem",
    System.Text.Encoding.UTF8.GetBytes("TestString"),
    true); // Store a secret
var secret = await storage.GetItemAsync("TestItem"); // Retrieve it
Console.WriteLine(System.Text.Encoding.UTF8.GetString(secret!)); // "TestString"

await storage.DeleteItemAsync("TestItem"); // Delete the secret
```

# Links
- Thanks to tmds for his [dbus library](https://github.com/tmds/Tmds.DBus)
- Inspiration: https://github.com/mitya57/FreeDesktopSecrets
- API Specification: https://specifications.freedesktop.org/- secret-service-spec/latest-single
