namespace Core.Input;

public record KeyboardLayout(string Name, IReadOnlyDictionary<KeyboardKey, char> Characters);
