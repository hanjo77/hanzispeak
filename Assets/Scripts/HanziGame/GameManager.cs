using System;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    [Header("Settings")]
    public HanziSpawner hanziSpawner;
    public GameObject[] characterPrefabs;
    public TMP_Text translationTextField;
    public TMP_Text pinyinTextField;
    public TMP_Text scoreTextField;
    public TMP_Text livesTextField;
    public GameObject explosionClip;
    public float explosionDuration;
    public Transform playerHead;

    // Singleton for easy access
    private HanziSpawner hanziSpawnerInstance;

    void Awake() => Instance = this;

    public void StartGame()
    {
        hanziSpawnerInstance = Instantiate(hanziSpawner);

        hanziSpawnerInstance.playerHead = playerHead;
        explosionClip.SetActive(false);
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

    public void PlayExplosion(Transform transform)
    {
        StartCoroutine(explosionLoader(transform));
    }

    IEnumerator explosionLoader(Transform transform)
    {
        explosionClip.transform.parent = transform.parent;
        explosionClip.transform.position = transform.position;
        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null)
        {
            Vector3 size = renderer.bounds.size;
            explosionClip.transform.position = new Vector3(
                transform.position.x + (size.x / 2),
                transform.position.y + (size.y / 2),
                transform.position.z + (size.z / 2)
            );
        }
        explosionClip.SetActive(true);
        yield return new WaitForSecondsRealtime(explosionDuration);
        explosionClip.SetActive(false);
    }
}