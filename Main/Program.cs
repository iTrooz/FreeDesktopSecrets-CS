SecretStorage storage = SecretStorage.FromSession();
await storage.Connect("TestApplication");
foreach (var item in await storage.ListItemKeys())
{
    Console.WriteLine(item);
}
