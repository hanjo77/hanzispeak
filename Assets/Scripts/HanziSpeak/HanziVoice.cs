using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public static class HanziVoiceDB
{
    private static Dictionary<string, HanziTranslations> _voices;
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/voices");
        if (jsonFile == null)
        {
            Debug.LogError("voice file not found!");
            return;
        }

        _voices = JsonConvert.DeserializeObject<Dictionary<string, HanziTranslations>>(jsonFile.text);

        Debug.Log($"Loaded {_voices.Count} voices");
        _isInitialized = true;
    }

    public static HanziTranslations GetVoice(string voiceKey)
    {
        if (!_isInitialized) Initialize();
        return _voices.TryGetValue(voiceKey, out var translations) ? translations : null;
    }

    public static IEnumerable<string> GetAllKeys()
    {
        if (!_isInitialized) Initialize();
        return _voices.Keys;
    }

    public static Dictionary<string, HanziTranslations> GetAllVoices()
    {
        if (!_isInitialized) Initialize();
        return _voices;
    }
}
