using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class Translator : MonoBehaviour
{
    [SerializeField] public string TranslationKey;
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
            try
            {
                textField.text = TranslationDB.GetTranslations(TranslationKey).GetTranslationString();
                if (textField.text.Contains("{points}"))
                {
                    StringBuilder builder = new StringBuilder(textField.text);
                    builder.Replace("{points}", GameManager.Instance.scoreTextField.text);
                    textField.text = builder.ToString();
                }
            }
            catch {
                UnityEngine.Debug.Log($"Failed to get translations for {TranslationKey}");
            }
        }
        if (dropDown != null)
        {
            for (int index = 0; index < dropDown.options.Count; index++) 
            {
                try
                {
                    HanziTranslations translations;
                    if (dropDown.name.Contains("VoiceDropdown"))
                    {
                        translations = HanziVoiceDB.GetVoice(HanziVoiceDB.GetAllKeys().ElementAt(index));
                    }
                    else if (dropDown.name.Contains("LevelDropdown"))
                    {
                        translations = HanziLevelDB.GetLevel(HanziLevelDB.GetAllKeys().ElementAt(index));
                    }
                    else
                    {
                        translations = HanziCategoryDB.GetCategory(HanziCategoryDB.GetAllKeys().ElementAt(index));
                    }
                    dropDown.options[index].text = translations.GetTranslationString();
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