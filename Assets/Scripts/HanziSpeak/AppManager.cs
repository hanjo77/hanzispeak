using System;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SocialPlatforms.Impl;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance;

    [Header("Views")]
    public MicWarningView micWarningView;
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
        MicWarningView();
    }

    public void MicWarningView()
    {
        HideAllViews();
        micWarningView.ShowView();
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

    public void AcceptMicWarningAndStartView()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
        StartView();
    }


    private void HideAllViews()
    {
        startView.HideView();
        gameView.HideView();
        settingsView.HideView();
        gameOverView.HideView();
        micWarningView.HideView();
        uiBackground.SetActive(true);
    }
}