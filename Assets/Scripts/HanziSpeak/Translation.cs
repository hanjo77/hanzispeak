using Newtonsoft.Json; // Requires Newtonsoft.Json package
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[System.Serializable]
public class Translation
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
        return PlayerPrefs.GetString("language", "en").ToLower() switch
        {
            "en" => en,
            "de" => de,
            "fr" => fr,
            "it" => it,
            "es" => es,
            "ja" => ja,
            "ko" => ko,
            "ru" => ru,
            _ => throw new ArgumentException($"Unsupported language code")
        };
    }
}

// Wrapper class for proper JSON parsing
[System.Serializable]
public class TranslationDatabaseWrapper
{
    public Dictionary<string, Translation> data;

    // Constructor for manual deserialization
    public TranslationDatabaseWrapper(Dictionary<string, Translation> dict)
    {
        data = dict;
    }
}

public static class TranslationDB
{
    private static Dictionary<string, Translation> _database;
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/hanziTranslations");
        if (jsonFile == null)
        {
            UnityEngine.Debug.LogError("Translation database not found!");
            return;
        }

        _database = JsonConvert.DeserializeObject<Dictionary<string, Translation>>(jsonFile.text);

        UnityEngine.Debug.Log($"Loaded {_database.Count} translation entries");
        _isInitialized = true;
    }

    public static Translation GetTranslations(string key)
    {
        if (!_isInitialized) Initialize();
        return _database.TryGetValue(key, out var data) ? data : null;
    }
}