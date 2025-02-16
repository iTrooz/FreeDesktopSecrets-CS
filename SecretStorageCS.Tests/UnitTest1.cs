namespace SecretStorageCS.Tests;

public class UnitTest1Test
{
    [Fact]
    public void Test1()
    {
        // Arrange
        int a = 1;
        int b = 1;
        int expected = 2;

        // Act
        int result = a + b;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Connectivity()
    {
        SecretStorage storage = new SecretStorage();
        await storage.Connect("TestApplication");
    }

    [Fact]
    public async Task SecretStorage()
    {
        SecretStorage storage = new SecretStorage();
        await storage.Connect("TestApplication");

        byte[] secretValue = System.Text.Encoding.UTF8.GetBytes("TestValue");

        await storage.CreateItem("TestItem", secretValue, true);
        Assert.Equal(secretValue, await storage.GetItem("TestItem"));
    }
}
