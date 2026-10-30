using Avalonia;
using Nexora.Render;

namespace Nexora.Tests.Render;

public class CardTransformTests
{
    private const double CardSize = 500;
    private const double HalfTurnPixels = Math.PI / 0.01;
    private const double FollowSeconds = 0.5;
    private readonly CardTransform transform = new CardTransform();

    [Fact]
    public void Rotate_DraggedRight_LeftEdgeComesCloser()
    {
        transform.Rotate(20, 0);
        transform.Advance(FollowSeconds);

        var matrix = transform.ToMatrix(CardSize);

        Assert.True(-matrix.Transform(new Point(-100, 0)).X > matrix.Transform(new Point(100, 0)).X);
    }

    [Fact]
    public void Rotate_DraggedDown_TopEdgeComesCloser()
    {
        transform.Rotate(0, 20);
        transform.Advance(FollowSeconds);

        var matrix = transform.ToMatrix(CardSize);

        Assert.True(-matrix.Transform(new Point(0, -100)).Y > matrix.Transform(new Point(0, 100)).Y);
    }

    [Fact]
    public void Rotate_HalfTurn_ShowsBackUnmirrored()
    {
        transform.Rotate(HalfTurnPixels, 0);
        transform.Advance(FollowSeconds);

        var point = transform.ToMatrix(CardSize).Transform(new Point(100, 50));

        Assert.True(transform.IsBackFacing);
        Assert.Equal(100, point.X, 3);
        Assert.Equal(50, point.Y, 3);
    }

    [Fact]
    public void Rotate_FullTurn_ShowsFront()
    {
        transform.Rotate(HalfTurnPixels, 0);
        transform.Rotate(HalfTurnPixels, 0);
        transform.Advance(FollowSeconds);

        Assert.False(transform.IsBackFacing);
    }

    [Fact]
    public void Advance_AfterRotate_FollowsGradually()
    {
        var followed = new CardTransform();
        followed.Rotate(50, 0);
        followed.Advance(FollowSeconds);
        transform.Rotate(50, 0);

        transform.Advance(0.016);

        double x = transform.ToMatrix(CardSize).Transform(new Point(100, 0)).X;
        double followedX = followed.ToMatrix(CardSize).Transform(new Point(100, 0)).X;
        Assert.InRange(x, followedX + 0.1, 99.9);
    }

    [Fact]
    public void Release_RightAfterDrag_KeepsSpinning()
    {
        transform.Grab(0);
        transform.Drag(10, 0, 0.01);
        transform.Drag(10, 0, 0.02);

        transform.Release(0.02);

        Assert.True(transform.IsSpinning);
    }

    [Fact]
    public void Release_AfterPause_Stops()
    {
        transform.Grab(0);
        transform.Drag(10, 0, 0.01);
        transform.Drag(10, 0, 0.02);

        transform.Release(0.5);

        Assert.False(transform.IsSpinning);
    }

    [Fact]
    public void Release_UnevenEventTimes_SpinsWithWindowAverageVelocity()
    {
        var even = new CardTransform();
        even.Grab(0);
        even.Drag(10, 0, 0.025);
        even.Drag(10, 0, 0.05);
        even.Release(0.05);
        transform.Grab(0);
        transform.Drag(10, 0, 0.049);
        transform.Drag(10, 0, 0.05);

        transform.Release(0.05);
        AdvanceForSeconds(transform, 0.3);
        AdvanceForSeconds(even, 0.3);

        Assert.Equal(even.ToMatrix(CardSize), transform.ToMatrix(CardSize));
    }

    [Fact]
    public void Release_OnlyOldMoves_Stops()
    {
        transform.Grab(0);
        transform.Drag(50, 0, 0.01);
        transform.Drag(1, 0, 0.2);

        transform.Release(0.2);

        Assert.False(transform.IsSpinning);
    }

    [Fact]
    public void Advance_SpinningForLongTime_Stops()
    {
        transform.Grab(0);
        transform.Drag(10, 0, 0.01);
        transform.Drag(10, 0, 0.02);
        transform.Release(0.02);

        AdvanceForSeconds(transform, 5);

        Assert.False(transform.IsSpinning);
    }

    [Fact]
    public void Advance_WhileDragging_DoesNotSettle()
    {
        transform.Grab(0);
        transform.Drag(50, 30, 0.01);

        AdvanceForSeconds(transform, 3);

        Assert.False(transform.IsAnimating);
        Assert.NotEqual(0, transform.ToMatrix(CardSize).M13, 4);
    }

    [Fact]
    public void Advance_StoppedShorterThanDelay_KeepsRotation()
    {
        transform.Rotate(50, 30);
        transform.Advance(FollowSeconds);

        bool isChanged = transform.Advance(0.4);

        Assert.False(isChanged);
        Assert.True(transform.IsAnimating);
    }

    [Fact]
    public void Advance_StoppedLongerThanDelay_SettlesFacingViewer()
    {
        transform.Rotate(50, 30);

        AdvanceForSeconds(transform, 3);

        var matrix = transform.ToMatrix(CardSize);
        Assert.False(transform.IsAnimating);
        Assert.Equal(0, matrix.M13, 4);
        Assert.Equal(0, matrix.M23, 4);
    }

    [Fact]
    public void Advance_RotatedAroundY_SettlesWithoutRotationAroundZ()
    {
        transform.Rotate(50, 0);

        AdvanceForSeconds(transform, 3);

        var point = transform.ToMatrix(CardSize).Transform(new Point(100, 50));
        Assert.Equal(100, point.X, 2);
        Assert.Equal(50, point.Y, 2);
    }

    [Fact]
    public void Advance_MostlyBackFacing_SettlesToBack()
    {
        transform.Rotate(HalfTurnPixels * 0.8, 0);

        AdvanceForSeconds(transform, 3);

        Assert.True(transform.IsBackFacing);
        Assert.False(transform.IsAnimating);
    }

    [Fact]
    public void Advance_SlightlyTwistedAroundZ_SettlesUntwisted()
    {
        RotateInSquare(20);

        AdvanceForSeconds(transform, 3);

        var point = transform.ToMatrix(CardSize).Transform(new Point(100, 50));
        Assert.Equal(100, point.X, 2);
        Assert.Equal(50, point.Y, 2);
    }

    [Fact]
    public void Advance_TwistedAroundZ_SettlesToQuarterTurn()
    {
        RotateInSquare(100);

        AdvanceForSeconds(transform, 3);

        var point = transform.ToMatrix(CardSize).Transform(new Point(100, 50));
        Assert.False(transform.IsAnimating);
        Assert.Equal(150, Math.Abs(point.X) + Math.Abs(point.Y), 2);
        Assert.Contains(Math.Round(Math.Abs(point.X), 2), new List<double>() { 50, 100 });
    }

    [Fact]
    public void Zoom_WheelUp_EnlargesCard()
    {
        transform.Zoom(1);
        transform.Advance(FollowSeconds);

        var point = transform.ToMatrix(CardSize).Transform(new Point(100, 50));

        Assert.Equal(120, point.X, 3);
        Assert.Equal(60, point.Y, 3);
    }

    [Fact]
    public void Zoom_FarBeyondLimits_ClampsScale()
    {
        transform.Zoom(100);
        transform.Advance(FollowSeconds);

        double x = transform.ToMatrix(CardSize).Transform(new Point(100, 0)).X;

        Assert.Equal(400, x, 3);
    }

    [Fact]
    public void Advance_AfterZoom_FollowsGradually()
    {
        transform.Zoom(1);

        transform.Advance(0.016);

        Assert.InRange(transform.ToMatrix(CardSize).Transform(new Point(100, 0)).X, 100.1, 119.9);
        Assert.True(transform.IsAnimating);
    }

    [Fact]
    public void Advance_ZoomedAndSettled_KeepsZoom()
    {
        transform.Zoom(1);
        transform.Rotate(50, 0);

        AdvanceForSeconds(transform, 3);

        Assert.Equal(120, transform.ToMatrix(CardSize).Transform(new Point(100, 0)).X, 2);
    }

    private void RotateInSquare(double side)
    {
        transform.Rotate(side, 0);
        transform.Rotate(0, side);
        transform.Rotate(-side, 0);
        transform.Rotate(0, -side);
    }

    private static void AdvanceForSeconds(CardTransform cardTransform, double seconds)
    {
        for(double elapsed = 0; elapsed < seconds; elapsed += 0.016)
        {
            cardTransform.Advance(0.016);
        }
    }
}
