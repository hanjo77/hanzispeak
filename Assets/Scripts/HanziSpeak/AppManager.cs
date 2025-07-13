using System.Collections;
using UnityEngine;
using UnityEngine.Android;

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

    [Header("Elements")]
    public GameObject uiBackground;



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
        PlayerPrefs.SetString("hanzifilter", HanziGroupDB.GetGroup(hanziGroup));
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

    public void TogglePassThrough()
    {
        string passthroughKey = "passthrough";
        if (PlayerPrefs.GetInt(passthroughKey) != 1)
        {
            SetClearFlagsForAllCameras(CameraClearFlags.SolidColor);
            OVRManager.instance.isInsightPassthroughEnabled = true;
            PlayerPrefs.SetInt(passthroughKey, 1);
        }
        else
        {
            SetClearFlagsForAllCameras(CameraClearFlags.Skybox);
            OVRManager.instance.isInsightPassthroughEnabled = false;
            PlayerPrefs.SetInt(passthroughKey, 0);
        }
    }

    private void SetClearFlagsForAllCameras(CameraClearFlags flags)
    {
        foreach (var cam in Camera.allCameras)
        {
            if (cam.isActiveAndEnabled)
            {
                cam.clearFlags = flags;
                cam.backgroundColor = new Color(0, 0, 0, 0);
            }
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