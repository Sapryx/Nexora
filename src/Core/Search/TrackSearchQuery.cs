using Core.Playback;

namespace Core.Search;

public class TrackSearchQuery
{
    private const int CharactersPerAllowedMismatch = 5;
    private const int MaxStackBufferLength = 512;
    private readonly string text;
    private readonly string[] words;

    public TrackSearchQuery(string rawQuery)
    {
        text = rawQuery.Trim().ToLowerInvariant();
        words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    public bool Matches(IAudioTrack audioTrack)
    {
        string title = audioTrack.Metadata.Title.ToLowerInvariant();
        string artists = audioTrack.Metadata.Artists.ToLowerInvariant();

        if(ContainsApproximately(title, text) || ContainsApproximately(artists, text))
        {
            return true;
        }

        return words.Length > 1 && words.All(word => ContainsApproximately(title, word) || ContainsApproximately(artists, word));
    }

    private static bool ContainsApproximately(string value, string pattern)
    {
        int maxMismatches = pattern.Length / CharactersPerAllowedMismatch;
        int rows = pattern.Length + 1;
        var buffer = rows * 3 <= MaxStackBufferLength ? stackalloc int[rows * 3] : new int[rows * 3];
        var beforePrevious = buffer.Slice(0, rows);
        var previous = buffer.Slice(rows, rows);
        var current = buffer.Slice(rows * 2, rows);

        for(int i = 0; i < rows; i++)
        {
            previous[i] = i;
        }

        if(previous[pattern.Length] <= maxMismatches)
        {
            return true;
        }

        for(int j = 1; j <= value.Length; j++)
        {
            current[0] = 0;

            for(int i = 1; i < rows; i++)
            {
                int substitutionCost = pattern[i - 1] == value[j - 1] ? 0 : 1;
                int distance = Math.Min(previous[i - 1] + substitutionCost, Math.Min(previous[i], current[i - 1]) + 1);

                if(i > 1 && j > 1 && pattern[i - 1] == value[j - 2] && pattern[i - 2] == value[j - 1])
                {
                    distance = Math.Min(distance, beforePrevious[i - 2] + 1);
                }

                current[i] = distance;
            }

            if(current[pattern.Length] <= maxMismatches)
            {
                return true;
            }

            var recycled = beforePrevious;
            beforePrevious = previous;
            previous = current;
            current = recycled;
        }

        return false;
    }
}
