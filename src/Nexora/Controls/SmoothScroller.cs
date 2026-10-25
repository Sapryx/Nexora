using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Nexora.Controls;

public sealed class SmoothScroller
{
    private double MaxOffset => Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

    private const double FallbackWheelStep = 100;
    private const double TimeConstantSeconds = 0.08;
    private const double StopDistance = 0.5;
    private const double FallbackFrameSeconds = 1.0 / 60;
    private readonly ScrollViewer scrollViewer;
    private readonly ItemsControl itemsControl;
    private double targetOffset;
    private double expectedOffset;
    private TimeSpan? lastFrameTime;
    private bool isAnimating;

    public SmoothScroller(ScrollViewer scrollViewer, ItemsControl itemsControl)
    {
        this.scrollViewer = scrollViewer;
        this.itemsControl = itemsControl;
        scrollViewer.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        scrollViewer.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        scrollViewer.TemplateApplied += OnScrollViewerTemplateApplied;
    }

    private void OnScrollViewerTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        var scrollBar = e.NameScope.Find<ScrollBar>("PART_VerticalScrollBar");

        if(scrollBar != null)
        {
            scrollBar.TemplateApplied += OnScrollBarTemplateApplied;
            scrollBar.ContextFlyout = null;
        }
    }

    private void OnScrollBarTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        SubscribePressed(e.NameScope.Find<Button>("PART_PageUpButton"), OnPageButtonPressed);
        SubscribePressed(e.NameScope.Find<Button>("PART_PageDownButton"), OnPageButtonPressed);
        SubscribePressed(e.NameScope.Find<Button>("PART_LineUpButton"), (_, args) => OnLineButtonPressed(args, 1));
        SubscribePressed(e.NameScope.Find<Button>("PART_LineDownButton"), (_, args) => OnLineButtonPressed(args, -1));
    }

    private static void SubscribePressed(Button? button, EventHandler<PointerPressedEventArgs> handler)
    {
        button?.AddHandler(InputElement.PointerPressedEvent, handler, RoutingStrategies.Tunnel);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool isScrolled = e.Key switch
        {
            Key.PageUp => ScrollByPages(1),
            Key.PageDown => ScrollByPages(-1),
            Key.Home => ScrollTo(0),
            Key.End => ScrollTo(MaxOffset),
            _ => false
        };

        if(isScrolled)
        {
            e.Handled = true;
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if(e.Delta.Y == 0)
        {
            return;
        }

        if(ScrollByItems(e.Delta.Y))
        {
            e.Handled = true;
        }
    }

    private void OnLineButtonPressed(PointerPressedEventArgs e, double delta)
    {
        if(e.GetCurrentPoint(null).Properties.IsLeftButtonPressed && ScrollByItems(delta))
        {
            e.Handled = true;
        }
    }

    private void OnPageButtonPressed(object? sender, PointerPressedEventArgs e)
    {
        var track = (sender as Visual)?.GetVisualParent() as Track;

        if(track?.Thumb == null || !e.GetCurrentPoint(track).Properties.IsLeftButtonPressed)
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

    public static double GetWheelTarget(double offset, double delta, double step)
    {
        bool isWheelNotch = delta == Math.Round(delta);

        if(!isWheelNotch)
        {
            return offset - delta * step;
        }

        double index = offset / step;
        double nearestIndex = Math.Round(index);

        if(Math.Abs(offset - nearestIndex * step) < StopDistance)
        {
            return (nearestIndex - delta) * step;
        }

        return (delta < 0 ? Math.Ceiling(index) - delta - 1 : Math.Floor(index) - delta + 1) * step;
    }

    public static double GetPageItemCount(double viewportHeight, double step)
    {
        return Math.Max(1, Math.Floor(viewportHeight / step));
    }

    private bool ScrollByPages(double delta)
    {
        return ScrollByItems(delta * GetPageItemCount(scrollViewer.Viewport.Height, GetWheelStep()));
    }

    private bool ScrollByItems(double delta)
    {
        double start = isAnimating ? targetOffset : scrollViewer.Offset.Y;
        return ScrollTo(GetWheelTarget(start, delta, GetWheelStep()));
    }

    private double GetWheelStep()
    {
        var item = itemsControl.GetRealizedContainers().FirstOrDefault(it => it.Bounds.Height > 0);
        return item?.Bounds.Height ?? FallbackWheelStep;
    }

    private bool ScrollTo(double offset)
    {
        var topLevel = TopLevel.GetTopLevel(scrollViewer);

        if(topLevel == null)
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
        double elapsed = lastFrameTime == null ? FallbackFrameSeconds : (time - lastFrameTime.Value).TotalSeconds;
        lastFrameTime = time;

        double current = scrollViewer.Offset.Y;
        var topLevel = TopLevel.GetTopLevel(scrollViewer);

        if(topLevel == null || Math.Abs(current - expectedOffset) > StopDistance)
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
