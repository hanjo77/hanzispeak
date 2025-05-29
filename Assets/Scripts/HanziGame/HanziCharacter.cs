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

    public void OnRecognized()
    {
        // Visual/Audio feedback
        isRecognized = true;
        GetComponent<MeshExploder>().Explode();
        GameManager.Instance.PlayExplosion(transform);
        StartCoroutine(WaitForRespawn(false));
    }

    public void OnFailed()
    {
        if (!isRecognized)
        {
            GameManager.Instance.PlayPinyinAudio("explosion");
            StartCoroutine(WaitForRespawn(true));
        }
    }

    public IEnumerator WaitForRespawn(bool removeLive)
    {
        GetComponent<MeshRenderer>().enabled = false;
        yield return new WaitForSecondsRealtime(2.5f);
        HanziSpawner.Instance.SpawnCharacter(removeLive);
    }
}
