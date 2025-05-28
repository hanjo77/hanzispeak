using System;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    [Header("Settings")]
    public HanziSpawner hanziSpawner;
    public GameObject[] characterPrefabs; // Assign your 3D character models
    public TMP_Text translationTextField;
    public TMP_Text pinyinTextField;
    public TMP_Text scoreTextField;
    public TMP_Text livesTextField;
    public Transform playerHead; // Assign XR Origin Camera

    // Singleton for easy access
    private HanziSpawner hanziSpawnerInstance;

    void Awake() => Instance = this;

    public void StartGame()
    {
        hanziSpawnerInstance = Instantiate(hanziSpawner);
        hanziSpawnerInstance.playerHead = playerHead;
    }

    public void StopGame()
    {
        Destroy(hanziSpawnerInstance);
    }

    public void SetLives(int currentLives)
    {
        livesTextField.text = currentLives.ToString();
    }

    public void SetScore(int score)
    {
        scoreTextField.text = score.ToString();
    }

    public void SetTranslation(HanziData hanziData)
    {
        translationTextField.text = hanziData.GetTranslationString();
    }

    public void SetPinyin(string pinyin)
    {
        pinyinTextField.text = pinyin;
    }

    public void PlayPinyinAudio(string pinyin)
    {
        // Load from Resources/Audio
        try
        {
            AudioClip clip = Resources.Load<AudioClip>($"Audio/{pinyin}");
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e);
        }
    }
}