using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class HanziSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject[] characterPrefabs; // Assign your 3D character models
    public Material hanziMaterial;
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
    public float checkInterval = 1.5f;

    // Track active characters
    private HanziCharacter activeHanzi;
    private Coroutine checkRoutine;

    public Dictionary<string, List<GameObject>> filteredWords;

    private int score = 0;
    private int currentLives;
    private bool isPlaying;
    private string activeWord;
    private List<string> wrongGuesses = new List<string>();


    void Awake() => Instance = this;

    void Start()
    {
        if (isPlaying) return;

        UnityEngine.Debug.Log("StartGame");
        HanziDB.Initialize();

        string desiredLevel = HanziLevelDB.GetAllKeys().ElementAt(PlayerPrefs.GetInt("level")).ToLower();
        string desiredClass = HanziCategoryDB.GetAllKeys().ElementAt(PlayerPrefs.GetInt("category")).ToLower();

        score = 0;
        currentLives = lives;
        GameManager.Instance.SetLives(currentLives);
        GameManager.Instance.SetScore(score);
        PrepareWordPrefabs(
            HanziLevelDB.FilterHanzi(desiredClass, desiredLevel)
        );
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
        if (ValidateWord(jsonResult))
        {
            GameManager.Instance.SetScore(++score);
            filteredWords.Remove(activeWord);
            activeHanzi.OnRecognized();
            if (filteredWords.Count <= 0)
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
        activeWord = filteredWords.Keys.ElementAt(UnityEngine.Random.Range(0, filteredWords.Keys.Count));
        GameObject prefab = SpawnWord(filteredWords[activeWord], spawnPos, 0.2f);
        foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterial = hanziMaterial;
        }

        // Instantiate random character
        GameObject newChar = Instantiate(
            prefab,
            spawnPos,
            Quaternion.LookRotation(spawnPos - playerHead.position) // Face player
        );

        try
        {
            HanziData hanziData = HanziDB.GetCharacter(activeWord);
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
            GameManager.Instance.PlayPinyinAudio($"{GetAudioFileName(activeWord)}");
        }

        // Add movement script
        newChar.AddComponent<ApproachingCharacter>().Init(playerHead, moveSpeed);
        activeHanzi = newChar.AddComponent<HanziCharacter>();
        activeHanzi.hanziText = activeWord;
        activeHanzi.transform.parent = transform;
    }

    public string GetAudioFileName(string hanzi)
    {
        List<string> hexCodes = new List<string>();

        foreach (char c in hanzi)
        {
            hexCodes.Add($"{((int)c):x4}");
        }

        return $"{HanziVoiceDB.GetAllKeys().ElementAt(PlayerPrefs.GetInt("voice"))}/u{string.Join("_", hexCodes)}";
    }

    public GameObject SpawnWord(List<GameObject> characterPrefabs, Vector3 startPosition, float spacing)
    {
        GameObject wordObj = new GameObject("Word_Spawned");

        List<GameObject> characters = new List<GameObject>();
        List<float> widths = new List<float>();

        float totalWidth = 0f;

        // Step 1: Instantiate all character prefabs, collect widths
        foreach (var prefab in characterPrefabs)
        {
            GameObject charObj = GameObject.Instantiate(prefab, wordObj.transform);
            float width = GetModelWidth(charObj);

            widths.Add(width);
            characters.Add(charObj);
            totalWidth += width + spacing;
        }

        // Adjust final spacing (remove last extra gap)
        totalWidth -= spacing;

        // Step 2: Layout characters with center alignment
        float startX = -totalWidth / 2f;
        float currentX = startX + (widths[0] / 2);

        for (int i = 0; i < characters.Count; i++)
        {
            characters[i].transform.localPosition = new Vector3(currentX, 0, 0);
            characters[i].AddComponent<MeshExploder>();

            currentX += widths[i] + spacing;
        }

        // Set final word position and reset rotation
        wordObj.transform.position = startPosition;
        wordObj.transform.rotation = Quaternion.identity;

        return wordObj;
    }

    private float GetModelWidth(GameObject go)
    {
        Renderer r = go.GetComponentInChildren<Renderer>();
        return r ? r.bounds.size.x : 1f;
    }

    bool ValidateWord(string validationJson)
    {
        Regex HanziRegex = new Regex(@"[\u4e00-\u9fff]+");

        TextAsset jsonFile = Resources.Load<TextAsset>("Text/hanziPinyin");
        if (jsonFile == null)
        {
            Debug.LogError("Pinyin database not found!");
            return false;
        }

        var pinyinData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonFile.text);
        if (!activeHanzi) return false;

        string fullWord = activeHanzi.hanziText;
        var expectedPinyinList = fullWord.Select(c =>
        {
            var match = pinyinData.FirstOrDefault(p => p.Value.Contains(c.ToString())).Key;
            return match ?? "";
        }).Where(p => !string.IsNullOrEmpty(p)).ToList();

        // Extract recognized Hanzi from result
        var matches = HanziRegex.Matches(validationJson);
        foreach (Match m in matches)
        {
            string recognized = m.Value;
            if (recognized.Length == 0) continue;

            var recognizedList = recognized.Select(c =>
            {
                var match = pinyinData.FirstOrDefault(p => p.Value.Contains(c.ToString())).Key;
                return match ?? "";
            }).Where(p => !string.IsNullOrEmpty(p)).ToList();

            // Order-agnostic matching: every expected normalized pinyin must appear in recognized set
            var normalizedExpected = expectedPinyinList
                .Select(p => NormalizePinyin(p))
                .ToList();

            var normalizedRecognized = recognizedList
                .Select(p => NormalizePinyin(p))
                .ToList();

            wrongGuesses.Clear();

            bool allFound = normalizedExpected.All(ne =>
            {
                bool found = normalizedRecognized.Any(nr => ComparePinyin(ne, nr).IsMatch);
                if (!found)
                {
                    // store missing expected syllable for feedback
                    if (!wrongGuesses.Contains(ne))
                        wrongGuesses.Add(ne);
                }
                return found;
            });

            if (allFound)
            {
                Debug.Log($"✅ Word match success: {recognized}");
                return true;
            }
        }

        foreach (var wrongGuess in wrongGuesses)
        {
            GetComponent<FlyInPinyin>().Fly(wrongGuess, false, playerHead.transform, activeHanzi);
        }

        Debug.Log($"❌ No match for word: {activeHanzi.hanziText}");
        return false;
    }

    public void PrepareWordPrefabs(List<string> words)
    {
        filteredWords = new Dictionary<string, List<GameObject>>();
        foreach (var word in words)
        {
            List<GameObject> charPrefabs = new List<GameObject>();

            foreach (char c in word)
            {
                string hex = ((int)c).ToString("x4");
                GameObject prefab = characterPrefabs.Where(c => c.name.Contains(hex)).ToArray().First();
                if (prefab)
                {
                    charPrefabs.Add(prefab);
                }
                else
                { 
                    Debug.LogWarning($"⚠️ Missing prefab for '{c}'");
                }
            }

            filteredWords[word] = charPrefabs;

            if (!isPlaying)
            {
                AppManager.Instance.voskEngine.OnTranscriptionResult = OnVoiceInput;
                isPlaying = true;
            }
        }
        SpawnCharacter(false);
    }

    /// <summary>
    /// Normalizes pinyin by removing tones and applying conservative, configurable softening rules.
    /// This preserves 'zh/ch/sh' distinctions but can collapse z/c/s and optionally l/r.
    /// </summary>
    private static string NormalizePinyin(string input, bool softenZcs = true, bool softenLr = true)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        string s = input.ToLowerInvariant();

        /* Remove numeric tone markers
        s = Regex.Replace(s, @"[1-5]", "");

        // Normalize common diacritics to base vowels
        s = Regex.Replace(s, "[āáǎà]", "a");
        s = Regex.Replace(s, "[ēéěè]", "e");
        s = Regex.Replace(s, "[īíǐì]", "i");
        s = Regex.Replace(s, "[ōóǒò]", "o");
        s = Regex.Replace(s, "[ūúǔùǖǘǚǜü]", "u"); */

        // Many speech engines confuse z / c / s — map them to 's' group if softenZcs == true
        s = s.Replace("zh", "s").Replace("ch", "s").Replace("sh", "s");

        // Alveolar group: z/c/s → s (Vosk often blurs these)
        s = s.Replace("z", "s").Replace("c", "s").Replace("j", "s").Replace("q", "s");

        // Alveolar group: z/c/s → s (Vosk often blurs these)
        s = s.Replace("w", "h");

        // Optionally merge l/r (many learners & engines confuse these)
        if (softenLr)
            s = s.Replace("r", "l");

        // Final cleanup: remove any unexpected whitespace
        s = s.Trim();

        return s;
    }

    /// <summary>
    /// Determines whether a target pinyin is fairly represented in guesses.
    /// Uses normalized comparison and a length-aware edit-distance threshold.
    /// </summary>
    public static PinyinMatchResult ComparePinyin(string targetPinyin, string guess)
    {
        var result = new PinyinMatchResult
        {
            IsMatch = false,
            IsSoftMatch = false,
            Distance = int.MaxValue,
            Note = ""
        };

        if (string.IsNullOrWhiteSpace(targetPinyin) || string.IsNullOrWhiteSpace(guess))
            return result;

        string normT = NormalizePinyin(targetPinyin);
        string normG = NormalizePinyin(guess);

        result.NormalizedTarget = normT;
        result.NormalizedGuess = normG;

        if (normT == normG)
        {
            result.IsMatch = true;
            result.Distance = 0;
            return result;
        }

        int dist = LevenshteinDistance(normT, normG);
        result.Distance = dist;

        int maxAllowed = normT.Length <= 3 ? 1 : 2;
        if (dist <= maxAllowed)
        {
            result.IsMatch = true;
            result.IsSoftMatch = true;
            result.Note = "within edit distance";
        }

        return result;
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

public struct PinyinMatchResult
{
    public bool IsMatch;          // overall success (after normalization + distance)
    public bool IsSoftMatch;      // matched only after normalization / distance tolerance
    public string NormalizedTarget;
    public string NormalizedGuess;
    public int Distance;
    public string Note;           // optional info like "merged zh→j" or "tone ignored"

    public override string ToString()
    {
        string type = IsSoftMatch ? "≈ (soft)" : "=";
        return $"{NormalizedGuess} {type} {NormalizedTarget}  dist={Distance}  note={Note}";
    }
}