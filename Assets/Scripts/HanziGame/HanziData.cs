using Newtonsoft.Json; // Requires Newtonsoft.Json package
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class HanziSource
{
    public string @class;
    public string season;
    public string episode;
    public string chapter;
}

[System.Serializable]
public class HanziData
{
    public string pinyin;
    public List<string> @class;  // 'class' is a reserved word
    public List<HanziSource> source;
    public Dictionary<string, string> translations;

    public string GetTranslation()
    {
        string lang = PlayerPrefs.GetString("language", "en").ToLower();
        return translations != null && translations.TryGetValue(lang, out var result)
            ? result
            : translations.TryGetValue("en", out var fallback) ? fallback : "?";
    }

    public bool HasClass(string className)
    {
        return @class != null && @class.Contains(className, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasSourceClass(string sourceClass)
    {
        return source != null && source.Exists(s =>
            s.@class.Equals(sourceClass, StringComparison.OrdinalIgnoreCase));
    }
}

// Wrapper class for proper JSON parsing
[System.Serializable]
public class HanziDatabaseWrapper
{
    public Dictionary<string, HanziData> data;

    // Constructor for manual deserialization
    public HanziDatabaseWrapper(Dictionary<string, HanziData> dict)
    {
        data = dict;
    }
}

public static class HanziDB
{
    private static Dictionary<string, HanziData> _database;
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/hanzi");
        if (jsonFile == null)
        {
            Debug.LogError("❌ Hanzi JSON file not found in Resources/Text/hanzi.json");
            return;
        }

        _database = JsonConvert.DeserializeObject<Dictionary<string, HanziData>>(jsonFile.text);
        Debug.Log($"✅ Loaded {_database.Count} Hanzi entries");
        _isInitialized = true;
    }

    public static HanziData GetCharacter(string hanzi)
    {
        if (!_isInitialized) Initialize();
        return _database.TryGetValue(hanzi, out var data) ? data : null;
    }

    public static IEnumerable<KeyValuePair<string, HanziData>> GetAll()
    {
        if (!_isInitialized) Initialize();
        return _database;
    }

    public static List<string> GetHanziByClass(string className)
    {
        if (!_isInitialized) Initialize();
        return _database
            .Where(kvp => kvp.Value.HasClass(className))
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public static List<string> GetHanziBySourceClass(string sourceClass)
    {
        if (!_isInitialized) Initialize();
        return _database
            .Where(kvp => kvp.Value.HasSourceClass(sourceClass))
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
