using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;

public class HanziSpawner : MonoBehaviour
{
    [Header("Settings")]
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

    // Singleton
    public static HanziSpawner Instance;

    private HanziCharacter activeHanzi;
    private bool isPlaying;
    private string activeWord;
    private int score;
    private int currentLives;

    private List<string> filteredWordKeys = new();
    private Dictionary<string, GameObject> prefabCache = new(); // cache loaded Hanzi prefabs
    private List<string> wrongGuesses = new();

    void Awake() => Instance = this;

    async void Start()
    {
        if (isPlaying) return;

        // Initialize Hanzi database
        HanziDB.Initialize();

        string desiredLevel = HanziLevelDB.GetAllKeys().ElementAt(PlayerPrefs.GetInt("level")).ToLower();
        string desiredClass = HanziCategoryDB.GetAllKeys().ElementAt(PlayerPrefs.GetInt("category")).ToLower();

        score = 0;
        currentLives = lives;
        GameManager.Instance.SetLives(currentLives);
        GameManager.Instance.SetScore(score);

        // Prepare filtered list (only keys)
        filteredWordKeys = HanziLevelDB.FilterHanzi(desiredClass, desiredLevel);
        Debug.Log($"Filtered {filteredWordKeys.Count} words for spawning.");

        AppManager.Instance.voskEngine.ResetRecognizer();
        AppManager.Instance.voskEngine.OnTranscriptionResult = OnVoiceInput;
        isPlaying = true;

        await SpawnCharacter(false);
    }

    void OnDestroy()
    {
        if (activeHanzi)
            Destroy(activeHanzi.gameObject);

        isPlaying = false;
        AppManager.Instance.voskEngine.OnTranscriptionResult = null;
        Debug.Log("Spawner stopped.");
    }

    private void OnVoiceInput(string jsonResult)
    {
        if (ValidateWord(jsonResult))
        {
            GameManager.Instance.SetScore(++score);
            filteredWordKeys.Remove(activeWord);
            activeHanzi.OnRecognized();

            if (filteredWordKeys.Count <= 0)
                AppManager.Instance.GameOverView(true);
        }
    }

    public async System.Threading.Tasks.Task SpawnCharacter(bool removeLive)
    {
        if (!isPlaying) return;

        if (removeLive)
        {
            currentLives--;
            if (currentLives < 0)
            {
                AppManager.Instance.GameOverView(false);
                return;
            }
            GameManager.Instance.SetLives(currentLives);
        }

        if (activeHanzi)
            Destroy(activeHanzi.gameObject);

        if (filteredWordKeys.Count == 0)
        {
            AppManager.Instance.GameOverView(true);
            return;
        }

        // Pick random word
        activeWord = filteredWordKeys[UnityEngine.Random.Range(0, filteredWordKeys.Count)];
        float randomAngle = UnityEngine.Random.Range(minAngle, maxAngle);
        Vector3 spawnDir = Quaternion.Euler(0, randomAngle, 0) * playerHead.forward;
        Vector3 spawnPos = playerHead.position + spawnDir * spawnDistance;
        spawnPos.y = playerHead.position.y;

        // Load prefabs dynamically per character
        GameObject wordObj = await LoadAndSpawnWord(activeWord, spawnPos, 0.2f);
        if (wordObj == null)
        {
            Debug.LogWarning($"⚠️ Could not load word '{activeWord}'");
            return;
        }

        // Instantiate
        GameObject newChar = Instantiate(wordObj, spawnPos, Quaternion.LookRotation(spawnPos - playerHead.position));
        newChar.transform.localScale = Vector3.one * 100f;

        // Set up data
        try
        {
            HanziData hanziData = HanziDB.GetCharacter(activeWord);
            if (PlayerPrefs.GetInt("translation") > 0)
                GameManager.Instance.SetTranslation(hanziData);
            if (PlayerPrefs.GetInt("pinyin") > 0)
                GameManager.Instance.SetPinyin(hanziData.pinyin);
        }
        catch
        {
            Debug.Log($"Hanzi data for {activeWord} not found.");
        }

        // Audio
        if (PlayerPrefs.GetInt("speak") > 0)
            GameManager.Instance.PlayPinyinAudio(GetAudioFileName(activeWord));

        // Movement
        newChar.AddComponent<ApproachingCharacter>().Init(playerHead, moveSpeed);
        activeHanzi = newChar.AddComponent<HanziCharacter>();
        activeHanzi.hanziText = activeWord;
        activeHanzi.transform.parent = transform;
    }

    private async System.Threading.Tasks.Task<GameObject> LoadAndSpawnWord(string word, Vector3 startPosition, float spacing)
    {
        var chars = new List<GameObject>();
        var widths = new List<float>();
        float totalWidth = 0f;

        foreach (char c in word)
        {
            string hex = ((int)c).ToString("x4");
            string key = $"Characters/u{hex}";

            if (!prefabCache.TryGetValue(key, out var prefab))
            {
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    prefab = handle.Result;
                    prefabCache[key] = prefab;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Missing Addressable prefab for '{c}' ({key})");
                    continue;
                }
            }

            GameObject charObj = Instantiate(prefab);
            charObj.GetComponentInChildren<Renderer>().sharedMaterial = hanziMaterial;
            charObj.transform.SetParent(null);
            float width = GetModelWidth(charObj);
            widths.Add(width);
            chars.Add(charObj);
            totalWidth += width + spacing;
        }

        if (chars.Count == 0)
            return null;

        // Layout horizontally
        totalWidth -= spacing;
        float startX = -totalWidth / 2f;
        float currentX = startX + (widths[0] / 2);

        GameObject wordObj = new GameObject($"Word_{word}");
        for (int i = 0; i < chars.Count; i++)
        {
            chars[i].transform.SetParent(wordObj.transform, false);
            chars[i].transform.localPosition = new Vector3(currentX, 0, 0);
            chars[i].AddComponent<MeshExploder>();
            currentX += widths[i] + spacing;
        }

        wordObj.transform.position = startPosition;
        wordObj.transform.rotation = Quaternion.identity;
        return wordObj;
    }

    private float GetModelWidth(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        return r ? r.bounds.size.x : 1f;
    }

    private string GetAudioFileName(string hanzi)
    {
        List<string> hexCodes = hanzi.Select(c => ((int)c).ToString("x4")).ToList();
        return $"{HanziVoiceDB.GetAllKeys().ElementAt(PlayerPrefs.GetInt("voice"))}/u{string.Join("_", hexCodes)}";
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

        Dictionary<string, List<string>> pinyinData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonFile.text);

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

    /// <summary>
    /// Normalizes pinyin by removing tones and applying conservative, configurable softening rules.
    /// This preserves 'zh/ch/sh' distinctions but can collapse z/c/s and optionally l/r.
    /// </summary>
    private static string NormalizePinyin(string input, bool softenZcs = true, bool softenLr = true)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        string s = input.ToLowerInvariant();

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