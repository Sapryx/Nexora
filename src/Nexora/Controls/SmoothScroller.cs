using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Nexora.Controls;

public sealed class SmoothScroller
{
    private const double WheelStep = 100;
    private const double TimeConstantSeconds = 0.08;
    private const double StopDistance = 0.5;
    private const double FallbackFrameSeconds = 1.0 / 60;
    private readonly ScrollViewer scrollViewer;
    private double targetOffset;
    private double expectedOffset;
    private TimeSpan? lastFrameTime;
    private bool isAnimating;
    
    private double MaxOffset => Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

    public SmoothScroller(ScrollViewer scrollViewer)
    {
        this.scrollViewer = scrollViewer;
        scrollViewer.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        scrollViewer.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if(e.Delta.Y == 0)
        {
            return;
        }

        double start = isAnimating ? targetOffset : scrollViewer.Offset.Y;
        if(ScrollTo(start - e.Delta.Y * WheelStep))
        {
            e.Handled = true;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var track = FindPressedTrack(e.Source as Visual);
        if(track?.Thumb is null || !e.GetCurrentPoint(track).Properties.IsLeftButtonPressed)
        {
            return;
        }

        double thumbLength = track.Thumb.Bounds.Height;
        double trackLength = track.Bounds.Height - thumbLength;
        if(trackLength <= 0)
        {
            return;
        }

        double ratio = Math.Clamp((e.GetPosition(track).Y - thumbLength / 2) / trackLength, 0, 1);
        if(ScrollTo(ratio * MaxOffset))
        {
            e.Handled = true;
        }
    }

    private static Track? FindPressedTrack(Visual? source)
    {
        var track = source?.FindAncestorOfType<Track>(true);
        if(track is null || track.Orientation != Orientation.Vertical)
        {
            return null;
        }

        bool isPageButton = IsWithin(source, track.IncreaseButton) || IsWithin(source, track.DecreaseButton);
        return isPageButton ? track : null;
    }

    private static bool IsWithin(Visual? source, Visual? button)
    {
        return source is not null && button is not null && (source == button || button.IsVisualAncestorOf(source));
    }

    private bool ScrollTo(double offset)
    {
        var topLevel = TopLevel.GetTopLevel(scrollViewer);
        if(topLevel is null)
        {
            return false;
        }

        targetOffset = Math.Clamp(offset, 0, MaxOffset);

        if(!isAnimating)
        {
            isAnimating = true;
            expectedOffset = scrollViewer.Offset.Y;
            lastFrameTime = null;
            topLevel.RequestAnimationFrame(OnFrame);
        }

        return true;
    }

    private void OnFrame(TimeSpan time)
    {
        double elapsed = lastFrameTime is null ? FallbackFrameSeconds : (time - lastFrameTime.Value).TotalSeconds;
        lastFrameTime = time;

        double current = scrollViewer.Offset.Y;
        var topLevel = TopLevel.GetTopLevel(scrollViewer);
        if(topLevel is null || Math.Abs(current - expectedOffset) > StopDistance)
        {
            isAnimating = false;
            return;
        }

        double distance = targetOffset - current;
        if(Math.Abs(distance) < StopDistance)
        {
            SetOffset(targetOffset);
            isAnimating = false;
            return;
        }

        double factor = 1 - Math.Exp(-elapsed / TimeConstantSeconds);
        SetOffset(current + distance * factor);
        topLevel.RequestAnimationFrame(OnFrame);
    }

    private void SetOffset(double offset)
    {
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, offset);
        expectedOffset = scrollViewer.Offset.Y;
    }
}
