using System.Text;
using Core.Playback;

namespace Core.Search;

public class TrackSearchQuery
{
    private const int CharactersPerAllowedMismatch = 5;
    private const int MaxStackBufferLength = 512;
    private readonly (string Text, string[] Words)[] variants;

    public TrackSearchQuery(string rawQuery, KeyboardLayoutTranslator layoutTranslator)
    {
        variants = layoutTranslator.Translate(rawQuery)
            .Prepend(rawQuery)
            .Select(Normalize)
            .Distinct()
            .Select(it => (it, it.Split(' ')))
            .ToArray();
    }

    public bool Matches(IAudioTrack audioTrack)
    {
        string title = Normalize(audioTrack.Metadata.Title);
        string artists = Normalize(audioTrack.Metadata.Artists);

        return variants.Any(it => Matches(it.Text, it.Words, title, artists));
    }

    private static bool Matches(string text, string[] words, string title, string artists)
    {
        if(ContainsApproximately(title, text) || ContainsApproximately(artists, text))
        {
            return true;
        }

        return words.Length > 1 && words.All(word => ContainsApproximately(title, word) || ContainsApproximately(artists, word));
    }

    private static string Normalize(string value)
    {
        string decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach(char character in decomposed)
        {
            if(char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if(char.IsWhiteSpace(character) && builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }
        }

        return builder.ToString().TrimEnd();
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
