using System;
using System.Diagnostics;
using UnityEngine;

public class HanziCharacter : MonoBehaviour
{
    public string hanziText; // Set in Inspector or spawner

    public void OnRecognized()
    {
        // Visual/Audio feedback
        HanziSpawner.Instance.SpawnCharacter(false);
        Destroy(gameObject);
    }
}
