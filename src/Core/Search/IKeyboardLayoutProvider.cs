namespace Core.Search;

public interface IKeyboardLayoutProvider
{
    IReadOnlyList<KeyboardLayout> GetLayouts();
}
