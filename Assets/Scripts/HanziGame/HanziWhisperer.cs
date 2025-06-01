using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Whisper;
using Whisper.Utils;
using static UnityEngine.InputSystem.InputControlScheme.MatchResult;
using static UnityEngine.Rendering.HableCurve;
using Match = System.Text.RegularExpressions.Match;
using TextAsset = UnityEngine.TextAsset;

public class HanziWhisperer : MonoBehaviour
{
    public static HanziWhisperer Instance;


    [Header("Settings")]
    public string currentChar;
    public HanziSpawner hanziSpawner;
    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;
    private WhisperStream _stream;
    private Dictionary<string, List<string>> pinyinData;

    // Singleton for easy access
    private HanziSpawner hanziSpawnerInstance;

    private void Awake()
    {
        Instance = this;
        TextAsset jsonFile = Resources.Load<TextAsset>("Text/hanziPinyin");
        if (jsonFile == null)
        {
            UnityEngine.Debug.LogError("Pinyin database not found!");
        }
        pinyinData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonFile.text);
        Start();
    }

    private void Start()
    {
        string[] devices = Microphone.devices;

        if (devices.Length == 0)
        {
            UnityEngine.Debug.LogError("No microphone devices found!");
            return;
        }

        // 2. Select the default/first microphone
        string primaryDevice = devices[0]; // First device is usually default
        UnityEngine.Debug.Log($"Using microphone: {primaryDevice}");

        if (microphoneRecord.SelectedMicDevice == null)
        {
            // 3. Initialize your recording component
            microphoneRecord.SelectedMicDevice = primaryDevice; // Set the device name
            microphoneRecord.OnVadChanged += MicrophoneRecord_OnVadChanged;
        }
        StartStream();
        microphoneRecord.StartRecord();
    }

    private async void StartStream()
    {
        _stream = await whisper.CreateStream(microphoneRecord);


        _stream.OnResultUpdated += OnResult;
        _stream.OnSegmentUpdated += OnSegmentUpdated;
        _stream.OnSegmentFinished += OnSegmentFinished; ;
        _stream.OnStreamFinished += OnStreamFinished;
        _stream.StartStream();
    }

    private void MicrophoneRecord_OnVadChanged(bool isSpeechDetected)
    {
        UnityEngine.Debug.Log($"Microphone detected voice {isSpeechDetected}");
    }

    private void OnResult(string result)
    {
        if (hanziSpawner != null && ValidateHanzi(result))
        {
            hanziSpawner.OnWhisperResult(true);
            UnityEngine.Debug.Log($"whisper segment validated {result} for {hanziSpawner.ActiveHanzi.name}");
        }
        UnityEngine.Debug.Log($"whisper result: {result}");
    }
    private void OnSegmentUpdated(WhisperResult segment)
    {
        if (hanziSpawner != null && ValidateHanzi(segment.Result.ToString()))
        {
            hanziSpawner.OnWhisperResult(true);
            UnityEngine.Debug.Log($"whisper segment validated {segment.Result} for {hanziSpawner.ActiveHanzi.name}");
        }
        UnityEngine.Debug.Log($"whisper segment updated: {segment.Result}");
    }
    private void OnSegmentFinished(WhisperResult segment)
    {
        if (hanziSpawner != null && ValidateHanzi(segment.Result.ToString()))
        {
            hanziSpawner.OnWhisperResult(true);
            UnityEngine.Debug.Log($"whisper segment validated {segment.Result} for {hanziSpawner.ActiveHanzi.name}");
        }
        UnityEngine.Debug.Log($"Segment finished: {segment.Result}");
    }
    private void OnStreamFinished(string finalResult)
    {
        if (hanziSpawner != null && ValidateHanzi(finalResult))
        {
            hanziSpawner.OnWhisperResult(true);
            UnityEngine.Debug.Log($"whisper segment validated {finalResult} for {hanziSpawner.ActiveHanzi.name}");
        }
        StartStream();
        UnityEngine.Debug.Log($"whisper final result: {finalResult}");
    }

    private bool ValidateHanzi(string validationJson)
    {
        Regex HanziRegex = new Regex(@"[\u4e00-\u9fff]+");
        string currentHanzi = HanziRegex.Matches(currentChar).First().Value;

        MatchCollection matches = HanziRegex.Matches(validationJson);

        foreach (Match match in matches)
        {
            foreach (char c in match.Value)
            {
                if (MatchPinyin(currentHanzi, c.ToString()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool MatchPinyin(string currentHanzi, string ResponseHanzi)
    {
        string currentPinyin = pinyinData.FirstOrDefault(x => x.Value.Contains(currentHanzi)).Key;
        string matchPinyin = pinyinData.FirstOrDefault(x => x.Value.Contains(ResponseHanzi)).Key;

        UnityEngine.Debug.Log($"... trying {matchPinyin} for {currentPinyin}");
        if (matchPinyin == currentPinyin)
        {
            UnityEngine.Debug.Log($"... with SUCCESS!!!");
            return true;
        }

        return false;
    }
}