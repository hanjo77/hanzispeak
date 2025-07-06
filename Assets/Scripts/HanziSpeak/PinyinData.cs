using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PinyinTranslations
{
    public string cn;
    public string en;
    public string de;
    public string fr;
    public string it;
    public string es;
    public string ja;
    public string ko;
    public string zh;
    public string ru;

    public string GetTranslationString()
    {
        switch (PlayerPrefs.GetString("language")) {
            case "de":
                return de;
            case "en":
                return en;
            case "fr":
                return fr;
            case "it":
                return it;
            case "es":
                return es;
            case "ja":
                return ja;
            case "ko":
                return ko;
            case "ru":
                return ru;
            case "zh":
                return zh;
            default:
                return en;
        }
    }
}

[System.Serializable]
public class PinyinData
{
    public PinyinTranslations hint;
    public List<string> siblings;
}

// Wrapper class for proper JSON parsing
[System.Serializable]
public class PinyinInfoDatabaseWrapper
{
    public Dictionary<string, PinyinData> data;

    // Constructor for manual deserialization
    public PinyinInfoDatabaseWrapper(Dictionary<string, PinyinData> dict)
    {
        data = dict;
    }
}

public static class PinyinInfoDB
{
    public static Dictionary<string, PinyinData> pinyins;
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/pinyinInfo");
        if (jsonFile == null)
        {
            UnityEngine.Debug.LogError("Pinyin database not found!");
            return;
        }

        pinyins = JsonConvert.DeserializeObject<Dictionary<string, PinyinData>>(jsonFile.text);

        // Method 2: Using Unity's JsonUtility (alternative)
        // var wrapper = JsonUtility.FromJson<HanziDatabaseWrapper>("{\"data\":" + jsonFile.text + "}");
        // _database = wrapper.data;

        UnityEngine.Debug.Log($"Loaded {pinyins.Count} pinyin entries");
        _isInitialized = true;
    }

    public static PinyinData GetCategory(string category)
    {
        if (!_isInitialized) Initialize();
        return pinyins.TryGetValue(category, out var data) ? data : null;
    }
}