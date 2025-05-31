
using System;
using UnityEngine;

public class GameOverView : AppView
{
    public GameObject title;
    public GameObject text;

    public void ChooseKeys(bool IsCompleted)
    {
        Translator translator = title.GetComponent<Translator>();
        translator.TranslationKey = IsCompleted ? "completeTitle" : "gameOverTitle";
        translator.UpdateTranslation();
        translator = text.GetComponent<Translator>();
        translator.TranslationKey = IsCompleted ? "completeText" : "gameOverText";
        translator.UpdateTranslation();

    }
}