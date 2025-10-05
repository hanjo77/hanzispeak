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

    private bool ValidateWord(string validationJson)
    {
        Regex HanziRegex = new Regex(@"[\u4e00-\u9fff]+");
        wrongGuesses = new List<string>();

        // Load pinyin dictionary
        TextAsset jsonFile = Resources.Load<TextAsset>("Text/hanziPinyin");
        if (jsonFile == null)
        {
            Debug.LogError("Pinyin database not found!");
            return false;
        }

        Dictionary<string, List<string>> pinyinData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonFile.text);

        if (!activeHanzi) return false;

        string fullWord = activeHanzi.hanziText;
        string[] expectedPinyinList = fullWord.Select(c =>
        {
            string match = pinyinData.FirstOrDefault(p => p.Value.Contains(c.ToString())).Key;
            return match ?? "";
        }).ToArray();

        // Extract all Hanzi from speech result
        MatchCollection recognizedMatches = HanziRegex.Matches(validationJson);
        foreach (Match match in recognizedMatches)
        {
            string recognized = match.Value;

            // Skip if length mismatch
            if (recognized.Length != fullWord.Length) continue;

            // Try to map each character
            string[] recognizedPinyinList = recognized.Select(c =>
            {
                string matchPin = pinyinData.FirstOrDefault(p => p.Value.Contains(c.ToString())).Key;
                return matchPin ?? "";
            }).ToArray();

            bool matchAll = true;
            for (int i = 0; i < expectedPinyinList.Length; i++)
            {
                if (recognizedPinyinList[i] != expectedPinyinList[i])
                {
                    matchAll = false;

                    string wrong = recognizedPinyinList[i];
                    if (!wrongGuesses.Contains(wrong))
                    {
                        wrongGuesses.Add(wrong);
                        GetComponent<FlyInPinyin>().Fly(wrong, false, playerHead.transform, activeHanzi);
                    }
                }
            }

            if (matchAll)
            {
                Debug.Log($"✅ Word match success: {recognized}");
                return true;
            }

            // Optionally: allow fuzzy match if most characters are correct
            int correctCount = expectedPinyinList.Zip(recognizedPinyinList, (exp, rec) => exp == rec).Count(b => b);
            if (correctCount >= fullWord.Length - 1) // allow one wrong
            {
                Debug.Log($"✅ Word fuzzy match (allowing 1 wrong): {recognized}");
                return true;
            }
        }

        Debug.Log($"❌ No match for word: {fullWord}");
        return false;
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
        s = s.Replace("c", "z").Replace("zh", "j").Replace("ch", "q").Replace("sh", "x");
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
