using Nexora.Controls;

namespace Nexora.Tests.Controls;

public class SmoothScrollerTests
{
    [Fact]
    public void GetWheelTarget_AlignedNotchDown_ScrollsOneItem()
    {
        Assert.Equal(200, SmoothScroller.GetWheelTarget(100, -1, 100));
    }

    [Fact]
    public void GetWheelTarget_AlignedNotchUp_ScrollsOneItem()
    {
        Assert.Equal(100, SmoothScroller.GetWheelTarget(200, 1, 100));
    }

    [Fact]
    public void GetWheelTarget_HalfItemOffsetNotchDown_ScrollsToNextItemTop()
    {
        Assert.Equal(100, SmoothScroller.GetWheelTarget(50, -1, 100));
    }

    [Fact]
    public void GetWheelTarget_HalfItemOffsetNotchUp_ScrollsToPartialItemTop()
    {
        Assert.Equal(100, SmoothScroller.GetWheelTarget(150, 1, 100));
    }

    [Fact]
    public void GetWheelTarget_SeveralNotches_ScrollsSeveralItems()
    {
        Assert.Equal(300, SmoothScroller.GetWheelTarget(50, -3, 100));
    }

    [Fact]
    public void GetWheelTarget_AlmostAlignedOffset_SnapsToNearestItemTop()
    {
        Assert.Equal(200, SmoothScroller.GetWheelTarget(100.2, -1, 100));
    }

    [Fact]
    public void GetWheelTarget_FractionalDelta_ScrollsProportionally()
    {
        Assert.Equal(80, SmoothScroller.GetWheelTarget(50, -0.3, 100));
    }
}
