using System;
using UnityEngine;
using UnityEngine.Android;

public class MicWarningView: AppView
{
    private void Start()
    {
        PinyinInfoDB.Initialize();
        Permission.RequestUserPermission(Permission.Microphone);
        UnityEngine.Debug.Log($"{PinyinInfoDB.GetCategory("x").hint.de}");
    }

}