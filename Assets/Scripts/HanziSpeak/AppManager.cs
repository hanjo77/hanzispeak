using System;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance;

    [Header("Views")]
    public StartView startView;
    public GameView gameView;
    public SettingsView settingsView;
    public GameOverView gameOverView;

    [Header("Elements")]
    public GameObject uiBackground;



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        StartView();
    }

    public void StartView()
    {
        HideAllViews();
        startView.ShowView();
    }

    public void PlayView()
    {
        HideAllViews();
        uiBackground.SetActive(false);
        gameView.ShowView();
    }

    public void SettingsView()
    {
        HideAllViews();
        settingsView.ShowView();
    }

    public void GameOverView(bool isCompleted = false)
    {
        HideAllViews();
        gameOverView.ChooseKeys(isCompleted);
        gameOverView.ShowView();
    }

    private void HideAllViews()
    {
        startView.HideView();
        gameView.HideView();
        settingsView.HideView();
        gameOverView.HideView();
        uiBackground.SetActive(true);
    }
}