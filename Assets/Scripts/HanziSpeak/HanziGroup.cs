using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

// Wrapper class for proper JSON parsing
[System.Serializable]
public class HanziGroupDatabaseWrapper
{
    public Dictionary<string, string> data;

    // Constructor for manual deserialization
    public HanziGroupDatabaseWrapper(Dictionary<string, string> dict)
    {
        data = dict;
    }
}

public static class HanziGroupDB
{
    public static Dictionary<string, string> Groups;
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/pinyinGroups");
        if (jsonFile == null)
        {
            UnityEngine.Debug.LogError("Hanzi group database not found!");
            return;
        }

        Groups = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonFile.text);

        UnityEngine.Debug.Log($"Loaded {Groups.Count} hanzi group entries");
        _isInitialized = true;
    }

    public static string GetGroup(string group)
    {
        if (!_isInitialized) Initialize();
        return Groups.TryGetValue(group, out var data) ? data : null;
    }
}