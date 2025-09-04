using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public static class HanziLevelDB
{
    private static Dictionary<string, HanziTranslations> _levels;
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/levels");
        if (jsonFile == null)
        {
            Debug.LogError("Hanzi levels file not found!");
            return;
        }

        _levels = JsonConvert.DeserializeObject<Dictionary<string, HanziTranslations>>(jsonFile.text);

        Debug.Log($"Loaded {_levels.Count} levels");
        _isInitialized = true;
    }

    public static HanziTranslations GetLevel(string levelKey)
    {
        if (!_isInitialized) Initialize();
        return _levels.TryGetValue(levelKey, out var translations) ? translations : null;
    }

    public static IEnumerable<string> GetAllKeys()
    {
        if (!_isInitialized) Initialize();
        return _levels.Keys;
    }

    public static Dictionary<string, HanziTranslations> GetAllLevels()
    {
        if (!_isInitialized) Initialize();
        return _levels;
    }
}
