using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Core.Search;

public class KeyboardLayoutTranslator
{
    private readonly ILogger logger;
    private readonly List<Dictionary<char, char>> translations = [];

    public KeyboardLayoutTranslator(IKeyboardLayoutProvider layoutProvider, ILogger<KeyboardLayoutTranslator> logger)
    {
        this.logger = logger;

        var layouts = GetDistinctLayouts(LoadLayouts(layoutProvider));

        foreach(var source in layouts)
        {
            foreach(var target in layouts)
            {
                if(source != target)
                {
                    var translation = CreateTranslation(source, target);
                    translations.Add(translation);
                    logger.Debug($"Keyboard layout translation {source.Name} -> {target.Name}: {translation.Count} characters");
                }
            }
        }

        if(translations.Count == 0)
        {
            logger.Info($"Keyboard layout switching in search is disabled: fewer than two distinct layouts");
        }
        else
        {
            logger.Info($"Built {translations.Count} keyboard layout translation tables");
        }
    }

    public IEnumerable<string> Translate(string text)
    {
        foreach(var translation in translations)
        {
            string translated = string.Concat(text.Select(it => translation.GetValueOrDefault(it, it)));

            if(translated != text)
            {
                yield return translated;
            }
        }
    }

    private IReadOnlyList<KeyboardLayout> LoadLayouts(IKeyboardLayoutProvider layoutProvider)
    {
        try
        {
            return layoutProvider.GetLayouts();
        }
        catch(Exception ex)
        {
            logger.Error(ex, $"Failed to read keyboard layouts");
            return [];
        }
    }

    private List<KeyboardLayout> GetDistinctLayouts(IReadOnlyList<KeyboardLayout> layouts)
    {
        List<KeyboardLayout> distinctLayouts = [];

        foreach(var layout in layouts)
        {
            var duplicate = distinctLayouts.Find(it => HaveSameCharacters(it, layout));

            if(layout.Characters.Count == 0)
            {
                logger.Info($"Skipped keyboard layout {layout.Name}: no characters");
            }
            else if(duplicate != null)
            {
                logger.Info($"Skipped keyboard layout {layout.Name}: same characters as {duplicate.Name}");
            }
            else
            {
                logger.Info($"Keyboard layout {layout.Name}: {layout.Characters.Count} characters");
                logger.Debug($"Keyboard layout {layout.Name} characters: {string.Concat(OrderByKey(layout.Characters).Select(it => it.Value))}");
                distinctLayouts.Add(layout);
            }
        }

        return distinctLayouts;
    }

    private static bool HaveSameCharacters(KeyboardLayout first, KeyboardLayout second)
    {
        return first.Characters.Count == second.Characters.Count &&
               first.Characters.All(it => second.Characters.TryGetValue(it.Key, out char character) && character == it.Value);
    }

    private static Dictionary<char, char> CreateTranslation(KeyboardLayout source, KeyboardLayout target)
    {
        Dictionary<char, char> translation = [];

        foreach(var (key, character) in OrderByKey(source.Characters))
        {
            if(target.Characters.TryGetValue(key, out char translated) && translated != character)
            {
                translation.TryAdd(character, translated);
            }
        }

        return translation;
    }

    private static IEnumerable<KeyValuePair<KeyboardKey, char>> OrderByKey(IReadOnlyDictionary<KeyboardKey, char> characters)
    {
        return characters.OrderBy(it => it.Key.ScanCode).ThenBy(it => it.Key.Shifted);
    }
}
