using Nexora.Views;

namespace Nexora.Tests.Views;

public class TilingWindowManagerTests
{
    [Theory]
    [InlineData("XDG_CURRENT_DESKTOP", "Hyprland", true)]
    [InlineData("XDG_CURRENT_DESKTOP", "sway:wlroots", true)]
    [InlineData("XDG_CURRENT_DESKTOP", "KDE", false)]
    [InlineData("XDG_CURRENT_DESKTOP", "GNOME", false)]
    [InlineData("XDG_CURRENT_DESKTOP", "not-i3", false)]
    [InlineData("XDG_SESSION_DESKTOP", "hyprland-uwsm", true)]
    [InlineData("DESKTOP_SESSION", "i3-with-shmlog", true)]
    [InlineData("DESKTOP_SESSION", null, false)]
    [InlineData("NIRI_SOCKET", "/run/user/1000/niri.sock", true)]
    [InlineData("SWAYSOCK", "", false)]
    public void IsPresent_RecognizesTilingSessions(string variable, string? value, bool expected)
    {
        Assert.Equal(expected, TilingWindowManager.IsPresent(name => name == variable ? value : null));
    }
}
