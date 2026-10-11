namespace Core.Search;

internal class QueryVariant
{
    public string Text { get; }
    public string[] Words { get; }

    public QueryVariant(string text)
    {
        Text = text;
        Words = text.Split(' ');
    }
}
