using Core.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Core.Tests.Search;

public static class TestKeyboardLayouts
{
    public static readonly KeyboardLayout English = Create("English (US)", "qwertyuiop[]asdfghjkl;'zxcvbnm,./`");
    public static readonly KeyboardLayout Russian = Create("Russian", "йцукенгшщзхъфывапролджэячсмитьбю.ё");

    public static KeyboardLayoutTranslator CreateTranslator(params KeyboardLayout[] layouts)
    {
        var layoutProviderMock = new Mock<IKeyboardLayoutProvider>();
        layoutProviderMock.Setup(it => it.GetLayouts()).Returns(layouts);

        return new KeyboardLayoutTranslator(layoutProviderMock.Object, NullLogger<KeyboardLayoutTranslator>.Instance);
    }

    private static KeyboardLayout Create(string name, string baseCharacters)
    {
        Dictionary<KeyboardKey, char> characters = [];

        for(int i = 0; i < baseCharacters.Length; i++)
        {
            characters[new KeyboardKey(i, false)] = baseCharacters[i];
            characters[new KeyboardKey(i, true)] = char.ToUpperInvariant(baseCharacters[i]);
        }

        return new KeyboardLayout(name, characters);
    }
}
