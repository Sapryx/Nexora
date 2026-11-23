namespace Core.Storage;

public static class OsCacheDirectory
{
    public static string Choose(bool isWindows, string localApplicationData, string? xdgCacheHome, string userProfile)
    {
        if(isWindows)
        {
            return localApplicationData;
        }

        return string.IsNullOrEmpty(xdgCacheHome) ? Path.Combine(userProfile, ".cache") : xdgCacheHome;
    }
}
