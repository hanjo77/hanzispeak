using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using UnityEngine.XR.Interaction.Toolkit;

public class HanziSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject[] characterPrefabs; // Assign your 3D character models
    public Transform playerHead; // Assign XR Origin Camera
    public float spawnDistance = 3f;
    public float moveSpeed = 1f;
    public float spawnInterval = 2f;

    [Header("Spawn Arc")]
    public float minAngle = 0;
    public float maxAngle = 0;

    [Header("Game Settings")]
    public int lives = 5;

    // Singleton for easy access
    public static HanziSpawner Instance;

    // Assign in Inspector
    public VoskSpeechToText voskEngine;
    public float checkInterval = 1.5f;

    // Track active characters
    private HanziCharacter activeHanzi;
    private Coroutine checkRoutine;

    public GameObject[] filteredCharacters;

    private int score;
    private int currentLives;
    private bool isPlaying;

    void Awake() => Instance = this;

    void Start()
    {
        if (isPlaying) return;

        UnityEngine.Debug.Log("StartGame");
        HanziDB.Initialize();
        isPlaying = true;
        voskEngine.OnTranscriptionResult = OnVoiceInput;
        List<GameObject> tmpChars = new List<GameObject>();
        score = 0;
        currentLives = lives;
        GameManager.Instance.SetLives(currentLives);
        GameManager.Instance.SetScore(score);
        foreach (var character in characterPrefabs)
        {
            string hanziFilter = PlayerPrefs.GetString("hanzifilter");
            if (hanziFilter.Length < 1 || hanziFilter.Contains(character.name))
            {
                tmpChars.Add(character);
            }
        }
        filteredCharacters = tmpChars.ToArray();
        SpawnCharacter(false);
    }

    void OnDestroy()
    {
        if (activeHanzi)
        {
            Destroy(activeHanzi.gameObject);
        }
        if (isPlaying)
        {
            voskEngine.OnTranscriptionResult = null;
            isPlaying = false;
        }
        UnityEngine.Debug.Log("Spawner StopGame");
    }

    void OnVoiceInput(string jsonResult)
    {
        if (ValidateHanzi(jsonResult))
        {
            GameManager.Instance.SetScore(score++);
            activeHanzi.OnRecognized();
        }
    }

    public void SpawnCharacter(bool removeLive)
    {
        if (!isPlaying)
        {
            return;
        }
        if (removeLive)
        {
            currentLives--;
            if (currentLives < 0)
            {
                AppManager.Instance.StartView();
                return;
            }
            else
            {
                GameManager.Instance.SetLives(currentLives);
            }
        }
        // Random position in front arc
        if (activeHanzi)
        {
            Destroy(activeHanzi.gameObject);
        }
        float randomAngle = UnityEngine.Random.Range(minAngle, maxAngle);
        Vector3 spawnDir = Quaternion.Euler(0, randomAngle, 0) * playerHead.forward;
        Vector3 spawnPos = playerHead.position + spawnDir * spawnDistance;
        spawnPos.y = playerHead.position.y; // Keep at eye level
        GameObject prefab = filteredCharacters[UnityEngine.Random.Range(0, filteredCharacters.Length)];

        // Instantiate random character
        GameObject newChar = Instantiate(
            prefab,
            spawnPos,
            Quaternion.LookRotation(playerHead.position - spawnPos) // Face player
        );

        try
        {
            HanziData hanziData = HanziDB.GetCharacter(prefab.name);
            if (PlayerPrefs.GetInt("translation") > 0)
            {
                GameManager.Instance.SetTranslation(hanziData);
            }

            if (PlayerPrefs.GetInt("pinyin") > 0)
            {
                GameManager.Instance.SetPinyin(hanziData.pinyin);
            }
        }
        catch
        {
            UnityEngine.Debug.Log($"Hanzi for {prefab.name} not found");
        }

        newChar.transform.localScale = new Vector3(100, 100, 100);
        if (PlayerPrefs.GetInt("speak") > 0)
        {
            GameManager.Instance.PlayPinyinAudio(prefab.name);
        }

        // Add movement script
        newChar.AddComponent<ApproachingCharacter>().Init(playerHead, moveSpeed);
        activeHanzi = newChar.AddComponent<HanziCharacter>();
        activeHanzi.hanziText = prefab.name;
    }

    private bool ValidateHanzi(string validationJson)
    {
        Regex HanziRegex = new Regex(@"[\u4e00-\u9fff]+");

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/hanziPinyin");
        if (jsonFile == null)
        {
            UnityEngine.Debug.LogError("Pinyin database not found!");
            return false;
        }
        Dictionary<string, List<string>> pinyinData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonFile.text);
        string currentHanzi = HanziRegex.Matches(activeHanzi.name).First().Value;
        string currentPinyin = pinyinData.FirstOrDefault(x => x.Value.Contains(currentHanzi)).Key;

        MatchCollection matches = HanziRegex.Matches(validationJson);

        foreach (Match match in matches)
        {
            string matchPinyin = pinyinData.FirstOrDefault(x => x.Value.Contains(match.Value)).Key;

            UnityEngine.Debug.Log($"... trying {matchPinyin} for {currentPinyin}");
            if (matchPinyin == currentPinyin)
            {
                UnityEngine.Debug.Log($"... with SUCCESS!!!");
                return true;
            }
        }

        return false;
    }
}


[Serializable]
public class PinyinDictionaryWrapper
{
    public List<PinyinEntry> entries;
}

[Serializable]
public class PinyinEntry
{
    public string hanzi;
    public List<string> pinyin;
}