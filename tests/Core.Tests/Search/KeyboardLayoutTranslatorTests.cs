using Core.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Core.Tests.Search;

public class KeyboardLayoutTranslatorTests
{
    private readonly KeyboardLayoutTranslator translator = TestKeyboardLayouts.CreateTranslator(TestKeyboardLayouts.English, TestKeyboardLayouts.Russian);

    [Theory]
    [InlineData("ghbdtn", "привет")]
    [InlineData("ыщтф", "sona")]
    [InlineData("Ghbdtn", "Привет")]
    [InlineData(";erb", "жуки")]
    [InlineData("rbyj 2", "кино 2")]
    public void Translate_TextInOneLayout_ReturnsTextInOtherLayout(string text, string expected)
    {
        Assert.Equal([expected], translator.Translate(text));
    }

    [Fact]
    public void Translate_TextWithoutLayoutCharacters_ReturnsNothing()
    {
        Assert.Empty(translator.Translate("123 456"));
    }

    [Fact]
    public void Translate_SingleLayout_ReturnsNothing()
    {
        var singleLayoutTranslator = TestKeyboardLayouts.CreateTranslator(TestKeyboardLayouts.English);

        Assert.Empty(singleLayoutTranslator.Translate("ghbdtn"));
    }

    [Fact]
    public void Translate_DuplicateLayouts_ReturnsNothing()
    {
        var duplicate = TestKeyboardLayouts.English with { Name = "Chinese (Simplified)" };
        var duplicateLayoutsTranslator = TestKeyboardLayouts.CreateTranslator(TestKeyboardLayouts.English, duplicate);

        Assert.Empty(duplicateLayoutsTranslator.Translate("ghbdtn"));
    }

    [Fact]
    public void Translate_ProviderThrows_ReturnsNothing()
    {
        var layoutProviderMock = new Mock<IKeyboardLayoutProvider>();
        layoutProviderMock.Setup(it => it.GetLayouts()).Throws(new DllNotFoundException());

        var failedTranslator = new KeyboardLayoutTranslator(layoutProviderMock.Object, NullLogger<KeyboardLayoutTranslator>.Instance);

        Assert.Empty(failedTranslator.Translate("ghbdtn"));
    }
}
