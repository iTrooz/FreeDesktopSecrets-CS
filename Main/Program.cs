SecretStorage storage = SecretStorage.FromSession();
await storage.ConnectAsync("TestApplication");
foreach (var item in await storage.ListItemKeysAsync())
{
    Console.WriteLine(item);
}
