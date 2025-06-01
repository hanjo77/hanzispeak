using Newtonsoft.Json;
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

    // Track active characters
    public HanziCharacter ActiveHanzi;
    private Coroutine checkRoutine;

    public List<GameObject> filteredCharacters;

    private int score = 0;
    private int currentLives;
    private bool isPlaying;
    private int activeFilterIndex;

    void Awake() => Instance = this;

    void Start()
    {
        if (isPlaying) return;

        UnityEngine.Debug.Log("StartGame");
        HanziDB.Initialize();
        isPlaying = true;
        score = 0;
        currentLives = lives;
        GameManager.Instance.SetLives(currentLives);
        GameManager.Instance.SetScore(score);
        GenerateFilteredCharacters();
        SpawnCharacter(false);
    }

    void OnDestroy()
    {
        if (ActiveHanzi)
        {
            Destroy(ActiveHanzi.gameObject);
        }
        if (isPlaying)
        {
            isPlaying = false;
        }
    }

    public void OnWhisperResult(bool success)
    {
        if (success)
        {
            GameManager.Instance.SetScore(++score);
            filteredCharacters.RemoveAt(activeFilterIndex);
            ActiveHanzi.OnRecognized();
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
        if (ActiveHanzi)
        {
            Destroy(ActiveHanzi.gameObject);
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

        HanziWhisperer.Instance.hanziSpawner = this;
        HanziWhisperer.Instance.currentChar = prefab.name;
        newChar.transform.localScale = new Vector3(100, 100, 100);
        if (PlayerPrefs.GetInt("speak") > 0)
        {
            GameManager.Instance.PlayPinyinAudio(prefab.name);
        }

        // Add movement script
        newChar.AddComponent<ApproachingCharacter>().Init(playerHead, moveSpeed);
        newChar.AddComponent<MeshExploder>();
        ActiveHanzi = newChar.AddComponent<HanziCharacter>();
        ActiveHanzi.hanziText = prefab.name;
        ActiveHanzi.transform.parent = transform;
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
}
