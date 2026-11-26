using Core.Storage;

namespace Core.Tests.Storage;

public class OsCacheDirectoryTests
{
    [Fact]
    public void Choose_Windows_ReturnsLocalApplicationData()
    {
        Assert.Equal("local", OsCacheDirectory.Choose(true, "local", "/xdg", "/home/user"));
    }

    [Fact]
    public void Choose_XdgCacheHomeSet_ReturnsIt()
    {
        Assert.Equal("/xdg", OsCacheDirectory.Choose(false, "local", "/xdg", "/home/user"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Choose_XdgCacheHomeUnset_ReturnsCacheInUserProfile(string? xdgCacheHome)
    {
        string directory = OsCacheDirectory.Choose(false, "local", xdgCacheHome, "/home/user");
        Assert.Equal(Path.Combine("/home/user", ".cache"), directory);
    }
}
