using System.Globalization;
using LibVLCSharp.Shared;

namespace Core.Playback;

public class VlcMediaEngine : IMediaEngine
{
    public float Position
    {
        get => mediaPlayer.Position;
        set => mediaPlayer.Position = value;
    }

    public long Time => mediaPlayer.Time;

    public int Volume
    {
        get => mediaPlayer.Volume;
        set => mediaPlayer.Volume = value;
    }

    public bool Mute
    {
        get => mediaPlayer.Mute;
        set => mediaPlayer.Mute = value;
    }

    public bool HoldsFile
    {
        get
        {
            var state = mediaPlayer.State;

            return state == VLCState.Playing || state == VLCState.Paused ||
                state == VLCState.Opening || state == VLCState.Buffering;
        }
    }

    private readonly LibVLC vlc;
    private readonly MediaPlayer mediaPlayer;
    private readonly Equalizer equalizer = new Equalizer();

    public event Action<float>? PositionChanged;
    public event Action? EndReached;
    public event Action? EncounteredError;
    public event Action<float>? VolumeChanged;
    public event Action<bool>? MuteChanged;

    public VlcMediaEngine(LibVLC vlc)
    {
        this.vlc = vlc;
        mediaPlayer = new MediaPlayer(vlc);

        mediaPlayer.PositionChanged += (_, args) => PositionChanged?.Invoke(args.Position);
        mediaPlayer.EndReached += (_, _) => Task.Run(() => EndReached?.Invoke());
        mediaPlayer.EncounteredError += (_, _) => Task.Run(() => EncounteredError?.Invoke());
        mediaPlayer.VolumeChanged += (_, args) => VolumeChanged?.Invoke(args.Volume * 100f);
        mediaPlayer.Muted += (_, _) => MuteChanged?.Invoke(true);
        mediaPlayer.Unmuted += (_, _) => MuteChanged?.Invoke(false);
    }

    public void Open(string audioPath, long startTimeMs, bool startPaused, double gainDb)
    {
        using var media = new Media(vlc, audioPath, FromType.FromPath);

        if(startTimeMs > 0)
        {
            double seconds = startTimeMs / 1000.0;
            media.AddOption($":start-time={seconds.ToString(CultureInfo.InvariantCulture)}");
        }

        if(startPaused)
        {
            media.AddOption(":start-paused");
        }

        equalizer.SetPreamp((float)gainDb);
        mediaPlayer.SetEqualizer(equalizer);
        mediaPlayer.Media = media;
        mediaPlayer.Play();
    }

    public void SetPause(bool isPaused)
    {
        mediaPlayer.SetPause(isPaused);
    }

    public void Stop()
    {
        mediaPlayer.Stop();
    }
}
