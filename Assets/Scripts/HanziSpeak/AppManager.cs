using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.InputSystem;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance;

    [Header("Views")]
    public MicWarningView micWarningView;
    public StartView startView;
    public GameView gameView;
    public SettingsView settingsView;
    public GameOverView gameOverView;
    public TrainingView trainingView;
    public GameObject quitWarning;

    [Header("Elements")]
    public GameObject uiBackground;

    [Header("Speech")]
    public VoskSpeechToText voskEngine;
    public float checkInterval = 1.5f;

    private float touchStartTime = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            StartView();
        }
        else
        {
            MicWarningView();
        }
        MicWarningView();
        HanziGroupDB.Initialize();
    }

    void Update()
    {
        var touch = Touchscreen.current?.primaryTouch;

        if (touch == null)
            return;

        if (touch.press.isPressed)
        {
            if (touchStartTime == 0f)
            {
                touchStartTime = Time.time;
                quitWarning.SetActive(false);
            }
            else
            {
                float duration = Time.time - touchStartTime;

                if (duration > 0.5f && !quitWarning.activeSelf)
                {
                    quitWarning.SetActive(true);
                    Debug.Log("Long press detected. Hold to quit...");
                }

                if (duration > 1.5f)
                {
                    Debug.Log("Quitting game...");
                    AppManager.Instance.SettingsView();
                    ResetTouch();
                }
            }
        }
        else if (touch.press.wasReleasedThisFrame)
        {
            ResetTouch();
        }
    }

    private void ResetTouch()
    {
        touchStartTime = 0f;
        quitWarning.SetActive(false);
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

    public void TrainingView()
    {
        HideAllViews();
        trainingView.ShowView();
    }

    public void StartTraining(string hanziGroup)
    {
        PlayView();
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
            StartCoroutine(WaitForMicPermissionThenStart());
        }
        else
        {
            StartView();
        }
    }

    private IEnumerator WaitForMicPermissionThenStart(float timeout = 10f)
    {
        float timer = 0f;

        while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            StartView();
        }
        else
        {
            MicWarningView(); // Show an error or go back
        }
    }

    private void HideAllViews()
    {
        startView.HideView();
        gameView.HideView();
        settingsView.HideView();
        gameOverView.HideView();
        micWarningView.HideView();
        trainingView.HideView();
        uiBackground.SetActive(true);
    }
}