namespace Core.Input;

public interface IKeyboardLayoutProvider
{
    IReadOnlyList<KeyboardLayout> GetLayouts();
}
