using System.Text;
using Core.Input;
using Core.Playback;

namespace Core.Search;

public class TrackSearchQuery
{
    private const int CharactersPerAllowedMismatch = 5;
    private const int MaxStackBufferLength = 512;
    private static readonly MatchKind[] BoundedKinds = [
        MatchKind.Exact,
        MatchKind.WholeWords,
        MatchKind.FieldPrefix,
        MatchKind.WordPrefix
    ];
    private readonly QueryVariant[] variants;

    public TrackSearchQuery(string rawQuery, KeyboardLayoutTranslator layoutTranslator)
    {
        variants = layoutTranslator.Translate(rawQuery)
            .Prepend(rawQuery)
            .Select(Normalize)
            .Distinct()
            .Select(it => new QueryVariant(it))
            .ToArray();
    }

    public TrackMatch? Match(IAudioTrack audioTrack)
    {
        string title = Normalize(audioTrack.Metadata.Title);
        string artists = Normalize(audioTrack.Metadata.JoinedArtists);
        TrackMatch? best = null;

        for(int i = 0; i < variants.Length; i++)
        {
            best = Best(best, Match(variants[i], i, title, artists));
        }

        return best;
    }

    private static TrackMatch? Match(QueryVariant variant, int layoutIndex, string title, string artists)
    {
        if(variant.Text == "")
        {
            return new TrackMatch(0, MatchKind.Exact, MatchField.Title, layoutIndex);
        }

        var titleMatch = MatchWhole(variant.Text, title, MatchField.Title, layoutIndex);
        var artistsMatch = MatchWhole(variant.Text, artists, MatchField.Artists, layoutIndex);
        var wordsMatch = MatchAllWords(variant.Words, title, artists, layoutIndex);

        return Best(Best(titleMatch, artistsMatch), wordsMatch);
    }

    private static TrackMatch? MatchWhole(string text, string value, MatchField field, int layoutIndex)
    {
        int typos = FindTypos(value, text, MatchBoundary.None, MatchBoundary.None);

        if(typos > MaxTypos(text))
        {
            return null;
        }

        var kind = BoundedKinds.FirstOrDefault(
            it => FindTypos(value, text, GetStart(it), GetEnd(it)) == typos,
            MatchKind.Substring
        );

        return new TrackMatch(typos, kind, field, layoutIndex);
    }

    private static TrackMatch? MatchAllWords(string[] words, string title, string artists, int layoutIndex)
    {
        if(words.Length < 2)
        {
            return null;
        }

        int typos = 0;
        var worstKind = MatchKind.Exact;

        foreach(string word in words)
        {
            var titleMatch = MatchWhole(word, title, MatchField.Title, layoutIndex);
            var artistsMatch = MatchWhole(word, artists, MatchField.Artists, layoutIndex);
            var wordMatch = Best(titleMatch, artistsMatch);

            if(wordMatch == null)
            {
                return null;
            }

            typos += wordMatch.Typos;

            if(wordMatch.Kind > worstKind)
            {
                worstKind = wordMatch.Kind;
            }
        }

        return new TrackMatch(typos, worstKind, MatchField.TitleAndArtists, layoutIndex);
    }

    private static TrackMatch? Best(TrackMatch? first, TrackMatch? second)
    {
        if(first == null)
        {
            return second;
        }

        return second != null && second.CompareTo(first) < 0 ? second : first;
    }

    private static int MaxTypos(string pattern)
    {
        return pattern.Length / CharactersPerAllowedMismatch;
    }

    private static MatchBoundary GetStart(MatchKind kind) => kind switch
    {
        MatchKind.Exact or MatchKind.FieldPrefix => MatchBoundary.Field,
        MatchKind.WholeWords or MatchKind.WordPrefix => MatchBoundary.Word,
        _ => MatchBoundary.None
    };

    private static MatchBoundary GetEnd(MatchKind kind) => kind switch
    {
        MatchKind.Exact => MatchBoundary.Field,
        MatchKind.WholeWords => MatchBoundary.Word,
        _ => MatchBoundary.None
    };

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

    private static int FindTypos(string value, string pattern, MatchBoundary start, MatchBoundary end)
    {
        int rows = pattern.Length + 1;
        var buffer = rows * 3 <= MaxStackBufferLength ? stackalloc int[rows * 3] : new int[rows * 3];
        var beforePrevious = buffer.Slice(0, rows);
        var previous = buffer.Slice(rows, rows);
        var current = buffer.Slice(rows * 2, rows);

        for(int i = 0; i < rows; i++)
        {
            previous[i] = i;
        }

        int best = IsEnd(value, 0, end) ? previous[pattern.Length] : int.MaxValue;

        for(int j = 1; j <= value.Length && best > 0; j++)
        {
            current[0] = IsStart(value, j, start) ? 0 : previous[0] + 1;

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

            if(IsEnd(value, j, end))
            {
                best = Math.Min(best, current[pattern.Length]);
            }

            var recycled = beforePrevious;
            beforePrevious = previous;
            previous = current;
            current = recycled;
        }

        return best;
    }

    private static bool IsStart(string value, int position, MatchBoundary boundary) => boundary switch
    {
        MatchBoundary.Field => position == 0,
        MatchBoundary.Word => position == 0 || value[position - 1] == ' ',
        _ => true
    };

    private static bool IsEnd(string value, int position, MatchBoundary boundary) => boundary switch
    {
        MatchBoundary.Field => position == value.Length,
        MatchBoundary.Word => position == value.Length || value[position] == ' ',
        _ => true
    };
}
