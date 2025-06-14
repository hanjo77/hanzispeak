﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

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

    public List<GameObject> filteredCharacters;

    private int score = 0;
    private int currentLives;
    private bool isPlaying;
    private int activeFilterIndex;
    private List<string> wrongGuesses = new List<string>();


    void Awake() => Instance = this;

    void Start()
    {
        if (isPlaying) return;

        UnityEngine.Debug.Log("StartGame");
        HanziDB.Initialize();
        isPlaying = true;
        voskEngine.OnTranscriptionResult = OnVoiceInput;
        score = 0;
        currentLives = lives;
        GameManager.Instance.SetLives(currentLives);
        GameManager.Instance.SetScore(score);
        GenerateFilteredCharacters();
        SpawnCharacter(false);
    }

    void OnDestroy()
    {
        if (activeHanzi)
        {
            Destroy(activeHanzi.gameObject);
        }
        isPlaying = false;
        voskEngine.OnTranscriptionResult = null;
        UnityEngine.Debug.Log("Spawner StopGame");
    }

    void OnVoiceInput(string jsonResult)
    {
        if (ValidateHanzi(jsonResult))
        {
            GameManager.Instance.SetScore(++score);
            filteredCharacters.RemoveAt(activeFilterIndex);
            activeHanzi.OnRecognized();
            if (filteredCharacters.Count <= 0)
            {
                AppManager.Instance.GameOverView(true);
            }
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
                AppManager.Instance.GameOverView(false);
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
        activeFilterIndex = UnityEngine.Random.Range(0, filteredCharacters.Count);
        GameObject prefab = filteredCharacters[activeFilterIndex];

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
        newChar.AddComponent<MeshExploder>();
        activeHanzi = newChar.AddComponent<HanziCharacter>();
        activeHanzi.hanziText = prefab.name;
        activeHanzi.transform.parent = transform;
    }

    private bool ValidateHanzi(string validationJson)
    {
        Regex HanziRegex = new Regex(@"[\u4e00-\u9fff]+");

        wrongGuesses = new List<string>();

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
            bool isCorrect = (matchPinyin == currentPinyin);
            if (!isCorrect)
            {
                if (wrongGuesses.Contains(matchPinyin))
                {
                    continue;
                }
                wrongGuesses.Add(matchPinyin);
            }
            UnityEngine.Debug.Log($"... trying {matchPinyin} for {currentPinyin}");
            if (isCorrect)
            {
                UnityEngine.Debug.Log($"... with SUCCESS!!!");
                return true;
            }
        }
        if (IsPinyinFairlyRepresented(currentPinyin, wrongGuesses))
        {
            return true;
        }
        foreach (string wrongGuess in wrongGuesses)
        {
            if (IsSomehowValid(wrongGuess, currentPinyin))
            {
                GetComponent<FlyInPinyin>().Fly(wrongGuess, false, playerHead.transform, activeHanzi);
            }
        }

        return false;
    }

    private void GenerateFilteredCharacters()
    {
        List<GameObject> tmpChars = new List<GameObject>();
        foreach (var character in characterPrefabs)
        {
            string hanziFilter = PlayerPrefs.GetString("hanzifilter");
            if (hanziFilter.Length < 1 || hanziFilter.Contains(character.name))
            {
                tmpChars.Add(character);
            }
        }
        filteredCharacters = tmpChars;
    }

    private bool IsSomehowValid(string pinyin, string targetPinyin)
    {
        if (pinyin == null || targetPinyin == null) return false;
        if (pinyin.Length < targetPinyin.Length) return false;

        for (int i = 0; i < pinyin.Length; ++i)
        {
            if (targetPinyin.IndexOf(pinyin[i]) > -1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPinyinFairlyRepresented(string targetPinyin, IEnumerable<string> guesses)
    {
        if (string.IsNullOrWhiteSpace(targetPinyin) || targetPinyin.Length < 2)
            return false;

        bool[] matches = new bool[targetPinyin.Length];
        for (int guessCounter = 0; guessCounter < guesses.Count(); ++guessCounter)
        {
            string guess = guesses.ElementAt(guessCounter);
            if (guess == targetPinyin)
                return true;
            if (guess == null || guess.Length != targetPinyin.Length)
                continue;
            for (int charCounter = 0; charCounter < targetPinyin.Length; ++charCounter)
            {
                if (guess.Length > charCounter && guess[charCounter] == (targetPinyin[charCounter]))
                {
                    matches[charCounter] = true;
                    continue;
                }
                else
                    return false;
            }
            return true;
        }
        for (int matchCount = 0; matchCount < matches.Length; ++matchCount)
        {
            if (!matches[matchCount])
            {
                return false;
            }
        }
        return true;
    }
}
