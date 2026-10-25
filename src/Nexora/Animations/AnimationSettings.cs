namespace Nexora.Animations;

public class AnimationSettings
{
    public required PopupAnimationSettings Popup { get; set; }
    public required ContextMenuAnimationSettings ContextMenu { get; set; }
    public required SegmentedControlAnimationSettings SegmentedControl { get; set; }
    public required TrackAnimationSettings Track { get; set; }
    public required DefaultAnimationSettings Default { get; set; }
}
