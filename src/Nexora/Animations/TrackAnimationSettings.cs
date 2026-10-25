namespace Nexora.Animations;

public class TrackAnimationSettings
{
    public required int HoverDuration { get; set; }
    public required string HoverColor { get; set; }
    public required TrackCoverAnimationSettings Cover { get; set; }
}
