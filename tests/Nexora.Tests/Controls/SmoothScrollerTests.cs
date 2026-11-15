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

    [Fact]
    public void GetPageItemCount_PartiallyVisibleItem_CountsOnlyFullyVisibleItems()
    {
        Assert.Equal(5, SmoothScroller.GetPageItemCount(550, 100));
    }

    [Fact]
    public void GetPageItemCount_ViewportSmallerThanItem_ReturnsOne()
    {
        Assert.Equal(1, SmoothScroller.GetPageItemCount(50, 100));
    }

    [Fact]
    public void GetRepeatAcceleration_LongList_CoversAllItemsInGivenTime()
    {
        Assert.Equal(20, SmoothScroller.GetRepeatAcceleration(120, 10, 3));
    }

    [Fact]
    public void GetRepeatAcceleration_ShortList_ReturnsZero()
    {
        Assert.Equal(0, SmoothScroller.GetRepeatAcceleration(10, 10, 3));
    }

    [Fact]
    public void GetRepeatItemCount_RepeatStarted_ScrollsOneItem()
    {
        Assert.Equal(1, SmoothScroller.GetRepeatItemCount(0, 10, 20));
    }

    [Fact]
    public void GetRepeatItemCount_SecondsHeld_AddsUniformlyAcceleratedDistance()
    {
        Assert.Equal(31, SmoothScroller.GetRepeatItemCount(2, 10, 5));
    }

    [Fact]
    public void GetItemIndex_PartiallyScrolledItem_ReturnsItemAtTop()
    {
        Assert.Equal(1, SmoothScroller.GetItemIndex(150, 100));
    }

    [Fact]
    public void GetItemIndex_AlmostAlignedOffset_ReturnsNextItem()
    {
        Assert.Equal(2, SmoothScroller.GetItemIndex(199.8, 100));
    }
}
