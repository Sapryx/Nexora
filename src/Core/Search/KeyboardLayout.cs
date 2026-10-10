namespace Core.Search;

public record KeyboardLayout(string Name, IReadOnlyDictionary<KeyboardKey, char> Characters);
