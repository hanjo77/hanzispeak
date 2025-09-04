using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HanziTranslations
{
    public string en;
    public string de;
    public string fr;
    public string it;
    public string es;
    public string ja;
    public string ko;
    public string ru;

    public string GetTranslationString()
    {
        switch (PlayerPrefs.GetString("language"))
        {
            case "de": return de;
            case "fr": return fr;
            case "it": return it;
            case "es": return es;
            case "ja": return ja;
            case "ko": return ko;
            case "ru": return ru;
            case "en":
            default:
                return en;
        }
    }
}
public static class HanziCategoryDB
{
    private static Dictionary<string, HanziTranslations> _categories;
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/hanziCategories");
        if (jsonFile == null)
        {
            Debug.LogError("Hanzi categories file not found!");
            return;
        }

        _categories = JsonConvert.DeserializeObject<Dictionary<string, HanziTranslations>>(jsonFile.text);

        Debug.Log($"Loaded {_categories.Count} categories");
        _isInitialized = true;
    }

    public static HanziTranslations GetCategory(string categoryKey)
    {
        if (!_isInitialized) Initialize();
        return _categories.TryGetValue(categoryKey, out var translations) ? translations : null;
    }

    public static IEnumerable<string> GetAllKeys()
    {
        if (!_isInitialized) Initialize();
        return _categories.Keys;
    }

    public static Dictionary<string, HanziTranslations> GetAllCategories()
    {
        if (!_isInitialized) Initialize();
        return _categories;
    }
}

