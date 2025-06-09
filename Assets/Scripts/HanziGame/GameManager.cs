using System;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

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
    public HapticImpulsePlayer leftController;
    public HapticImpulsePlayer rightController;
    public string failedHanzi;
    public string successfulHanzi;

    // Singleton for easy access
    private HanziSpawner hanziSpawnerInstance;

    void Awake () {
        Instance = this;
    }

    public void StartGame()
    {
        hanziSpawnerInstance = Instantiate(hanziSpawner);
        hanziSpawnerInstance.transform.parent = GetComponent<GameView>().transform;
        hanziSpawnerInstance.playerHead = playerHead;
        explosionClip.SetActive(false);
    }

    public void StopGame()
    {
        Destroy(hanziSpawnerInstance);
        foreach (var obj in GameObject.FindGameObjectsWithTag("PinyinHintClone"))
        {
            Destroy(obj);
        }
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
            float volume = pinyin.Length > 1 ? .6f : 1.0f;
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e);
        }
    }

    public void ShakeCamera()
    {
        GetComponent<CameraShake>().StartShake();
    }

    public void PlayExplosion(Transform transform)
    {
        StartCoroutine(explosionLoader(transform));
    }

    public void AddSuccessChar(string text)
    {
        if (!GameManager.Instance.successfulHanzi.Contains(text))
        {
            GameManager.Instance.successfulHanzi += text;
        }
        if (GameManager.Instance.failedHanzi.Contains(text))
        {
            GameManager.Instance.failedHanzi = GameManager.Instance.failedHanzi.Replace(text, string.Empty);
        }
        UpdateLearnedChars();
    }

    public void AddFailedChar(string text)
    {

        if (!GameManager.Instance.failedHanzi.Contains(text))
        {
            GameManager.Instance.failedHanzi += text;
        }
        if (GameManager.Instance.successfulHanzi.Contains(text))
        {
            GameManager.Instance.successfulHanzi = GameManager.Instance.successfulHanzi.Replace(text, string.Empty);
        }
        UpdateLearnedChars();
    }

    private void UpdateLearnedChars()
    {
        PlayerPrefs.SetString("failedHanzi", GameManager.Instance.failedHanzi);
        PlayerPrefs.SetString("successfulHanzi", GameManager.Instance.successfulHanzi);
        PlayerPrefs.Save();
    }

    public void VibrateControllers(float amplitude, float duration)
    {
        leftController.SendHapticImpulse(amplitude, duration);
        rightController.SendHapticImpulse(amplitude, duration);
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
        PlayPinyinAudio("explosion-small");
        yield return new WaitForSecondsRealtime(explosionDuration);
        explosionClip.SetActive(false);
    }
}