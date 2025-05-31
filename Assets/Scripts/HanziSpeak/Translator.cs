using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class Translator : MonoBehaviour
{
    [SerializeField] private string translationKey;
    [SerializeField] private TMP_Text textField;
    [SerializeField] private TMP_Dropdown dropDown;

    void Start()
    {
        TranslationDB.Initialize();
        HanziCategoryDB.Initialize();
        textField = gameObject.GetComponent<TMP_Text>();
        dropDown = gameObject.GetComponent<TMP_Dropdown>();
        UpdateTranslation();
    }
    public void UpdateTranslation()
    {
        if (textField != null)
        {
            textField.text = TranslationDB.GetTranslations(translationKey).GetTranslationString();
            if (textField.text.Contains("{points}"))
            {
                StringBuilder builder = new StringBuilder(textField.text);
                builder.Replace("{points}", GameManager.Instance.scoreTextField.text);
                textField.text = builder.ToString();
            }
        }
        if (dropDown != null)
        {
            for (int index = 0; index < dropDown.options.Count; index++) 
            {
                try
                {
                    HanziCategory hanziCategory = HanziCategoryDB.GetCategory(index.ToString());
                    dropDown.options[index].text = hanziCategory.title.GetTranslationString();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                }
            }
            dropDown.RefreshShownValue();
        }
    }
}