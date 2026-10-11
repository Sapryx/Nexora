namespace Core.Search;

public enum MatchKind
{
    Exact,
    WholeWords,
    FieldPrefix,
    WordPrefix,
    Substring
}
