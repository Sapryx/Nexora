using Core.Playback;

namespace Core.Search;

public class TrackSearchQuery
{
    private const int CharactersPerAllowedMismatch = 5;
    private readonly string text;
    private readonly int maxMismatches;

    public TrackSearchQuery(string rawQuery)
    {
        text = rawQuery.Trim().ToLowerInvariant();
        maxMismatches = text.Length / CharactersPerAllowedMismatch;
    }

    public bool Matches(IAudioTrack audioTrack)
    {
        bool titleMatches = ContainsApproximately(audioTrack.Metadata.Title);
        bool artistsMatch = ContainsApproximately(audioTrack.Metadata.Artists);
        
        return titleMatches || artistsMatch;
    }

    private bool ContainsApproximately(string field)
    {
        string value = field.ToLowerInvariant();

        for(int start = 0; start <= value.Length - text.Length; start++)
        {
            int mismatches = 0;

            for(int i = 0; i < text.Length && mismatches <= maxMismatches; i++)
            {
                if(value[start + i] != text[i])
                {
                    mismatches++;
                }
            }

            if(mismatches <= maxMismatches)
            {
                return true;
            }
        }

        return false;
    }
}
