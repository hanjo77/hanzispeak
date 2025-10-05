using System;
using UnityEngine;

public class MicWarningView: AppView
{
    private void Start()
    {
        PinyinInfoDB.Initialize();
        UnityEngine.Debug.Log($"{PinyinInfoDB.GetCategory("x").hint.de}");
    }

}