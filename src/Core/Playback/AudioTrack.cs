namespace Core.Playback;

public class AudioTrack : IAudioTrack
{
    public string AudioPath { get; }
    public Metadata Metadata { get; }

    public AudioTrack(string audioPath, Metadata metadata)
    {
        AudioPath = audioPath;
        Metadata = metadata;
    }

    public override string ToString()
    {
        bool titleIsSpecified = Metadata.Title != "";
        bool artistsAreSpecified = Metadata.Artists != "";

        if(titleIsSpecified && artistsAreSpecified)
        {
            return $"{Metadata.Artists} - {Metadata.Title}";
        }
        else
        {
            return Path.GetFileNameWithoutExtension(AudioPath);
        }
    }
}
