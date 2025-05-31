using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

public class HanziCharacter : MonoBehaviour
{
    public string hanziText;
    public float fadeTime = 1.0f;
    private bool isRecognized;
    private bool hasFailed;

    public void OnRecognized()
    {
        // Visual/Audio feedback
        if (!GameManager.Instance.successfulHanzi.Contains(hanziText))
        {
            GameManager.Instance.successfulHanzi += hanziText;
        }
        isRecognized = true;
        GetComponent<MeshExploder>().Explode();
        GameManager.Instance.VibrateControllers(.5f, .1f);
        GameManager.Instance.PlayExplosion(transform);
        StartCoroutine(WaitForRespawn(false));
    }

    public void OnFailed()
    {
        if (!isRecognized && !hasFailed)
        {
            if (!GameManager.Instance.failedHanzi.Contains(hanziText))
            {
                GameManager.Instance.failedHanzi += hanziText;
            }
            GameManager.Instance.VibrateControllers(1f, .5f);
            GameManager.Instance.PlayPinyinAudio("explosion");
            StartCoroutine(WaitForRespawn(true));
            hasFailed = true;
        }
    }

    public IEnumerator WaitForRespawn(bool removeLive)
    {
        if (removeLive)
        {
            GameManager.Instance.ShakeCamera();
        }
        GetComponent<MeshRenderer>().enabled = false;
        yield return new WaitForSecondsRealtime(2f);
        HanziSpawner.Instance.SpawnCharacter(removeLive);
    }
}
