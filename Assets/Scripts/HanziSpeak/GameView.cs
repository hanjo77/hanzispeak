using System;
using UnityEngine;

public class GameView : AppView
{
    public GameObject hanziInfo;

    public override void ShowView()
    {
        base.ShowView();
        GameManager.Instance.StartGame();
        hanziInfo.SetActive(true);
    }
    public override void HideView()
    {
        hanziInfo.SetActive(false);
        GameManager.Instance?.StopGame();
        base.HideView();
    }
}