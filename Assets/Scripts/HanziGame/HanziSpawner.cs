using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

public class HanziSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject[] characterPrefabs; // Assign your 3D character models
    public Transform playerHead; // Assign XR Origin Camera
    public Material hanziMaterial;
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
        AppManager.Instance.voskEngine.OnTranscriptionResult = OnVoiceInput;
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
        AppManager.Instance.voskEngine.OnTranscriptionResult = null;
        UnityEngine.Debug.Log("Spawner StopGame");
    }

    void OnVoiceInput(string jsonResult)
    {
        if (ValidateHanzi(jsonResult) && filteredCharacters.Count > activeFilterIndex)
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
        prefab.GetComponent<Renderer>().material = hanziMaterial;

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
        activeHanzi.Init();
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
        if (activeHanzi)
        {
            string currentHanzi = HanziRegex.Matches(activeHanzi.name).First().Value;
            string currentPinyin = pinyinData.FirstOrDefault(x => x.Value.Contains(currentHanzi)).Key;

            MatchCollection matches = HanziRegex.Matches(validationJson);

            foreach (Match match in matches)
            {
                StringCollection chars = new StringCollection();
                if (match.Value.Length > 1)
                {
                    foreach (char chr in match.Value)
                    {
                        chars.Add(chr.ToString());
                    }
                }
                else
                {
                    chars.Add(match.Value);
                }
                foreach (string chr in chars)
                {
                    string matchPinyin = pinyinData.FirstOrDefault(x => x.Value.Contains(chr)).Key;
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
            }
            if (IsPinyinFairlyRepresented(currentPinyin, wrongGuesses))
            {
                return true;
            }
            foreach (string wrongGuess in wrongGuesses)
            {
                if (IsSomehowValid(wrongGuess, currentPinyin))
                {
                    return true;
                }
            }
            foreach (string wrongGuess in wrongGuesses)
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

    /// <summary>
    /// Checks if the target pinyin is fairly represented in the list of guesses,
    /// allowing for tone differences and common pronunciation errors.
    /// </summary>
    public static bool IsPinyinFairlyRepresented(string targetPinyin, IEnumerable<string> guesses)
    {
        if (string.IsNullOrWhiteSpace(targetPinyin) || targetPinyin.Length < 2)
            return false;

        string normalizedTarget = NormalizePinyin(targetPinyin);

        foreach (string guess in guesses)
        {
            if (string.IsNullOrWhiteSpace(guess))
                continue;

            string normalizedGuess = NormalizePinyin(guess);

            // Exact match
            if (normalizedGuess == normalizedTarget)
                return true;

            // Fuzzy match within 2 edits
            int distance = LevenshteinDistance(normalizedTarget, normalizedGuess);
            if (distance <= 1)
            {
                Debug.Log($"[Pinyin Match] '{normalizedGuess}' is close to '{normalizedTarget}' (distance: {distance})");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Normalizes pinyin by removing tones and applying common pronunciation mappings.
    /// </summary>
    private static string NormalizePinyin(string input)
    {
        string s = input.ToLowerInvariant();

        // Remove numeric tones
        s = Regex.Replace(s, @"[1-5]", "");

        // Handle confusing initials and substitutions
        s = s.Replace("zh", "j").Replace("ch", "q").Replace("sh", "x");
        s = s.Replace("z", "j").Replace("c", "q").Replace("s", "x");

        return s;
    }

    /// <summary>
    /// Calculates Levenshtein edit distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string s, string t)
    {
        if (s == t) return 0;
        if (s.Length == 0) return t.Length;
        if (t.Length == 0) return s.Length;

        int[,] d = new int[s.Length + 1, t.Length + 1];

        for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= t.Length; j++) d[0, j] = j;

        for (int i = 1; i <= s.Length; i++)
        {
            for (int j = 1; j <= t.Length; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s.Length, t.Length];
    }

}
