using Newtonsoft.Json; // Requires Newtonsoft.Json package
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[System.Serializable]
public class HanziData
{
    public string pinyin;
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
            UnityEngine.Debug.LogError("Hanzi database not found!");
            return;
        }

        _database = JsonConvert.DeserializeObject<Dictionary<string, HanziData>>(jsonFile.text);

        // Method 2: Using Unity's JsonUtility (alternative)
        // var wrapper = JsonUtility.FromJson<HanziDatabaseWrapper>("{\"data\":" + jsonFile.text + "}");
        // _database = wrapper.data;

        UnityEngine.Debug.Log($"Loaded {_database.Count} hanzi entries");
        _isInitialized = true;
    }

    public static HanziData GetCharacter(string hanzi)
    {
        if (!_isInitialized) Initialize();
        return _database.TryGetValue(hanzi, out var data) ? data : null;
    }
}