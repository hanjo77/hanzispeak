using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor; // Requires Newtonsoft.Json package
using UnityEngine;

[System.Serializable]
public class HanziCategory
{
    public string title;
    public string hanzi;
}

// Wrapper class for proper JSON parsing
[System.Serializable]
public class HanziCategoryDatabaseWrapper
{
    public Dictionary<string, HanziCategory> data;

    // Constructor for manual deserialization
    public HanziCategoryDatabaseWrapper(Dictionary<string, HanziCategory> dict)
    {
        data = dict;
    }
}

public static class HanziCategoryDB
{
    public static Dictionary<string, HanziCategory> Categories;
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/hanziCategories");
        if (jsonFile == null)
        {
            UnityEngine.Debug.LogError("Hanzi database not found!");
            return;
        }

        Categories = JsonConvert.DeserializeObject<Dictionary<string, HanziCategory>>(jsonFile.text);

        // Method 2: Using Unity's JsonUtility (alternative)
        // var wrapper = JsonUtility.FromJson<HanziDatabaseWrapper>("{\"data\":" + jsonFile.text + "}");
        // _database = wrapper.data;

        UnityEngine.Debug.Log($"Loaded {Categories.Count} hanzi entries");
        _isInitialized = true;
    }

    public static HanziCategory GetCategory(string category)
    {
        if (!_isInitialized) Initialize();
        return Categories.TryGetValue(category, out var data) ? data : null;
    }
}