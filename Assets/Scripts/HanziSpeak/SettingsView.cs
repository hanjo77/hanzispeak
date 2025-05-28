using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class SettingsView : AppView
{
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown categoryDropdown;
    public UnityEngine.UI.Toggle pinyinToggle;
    public UnityEngine.UI.Toggle translationToggle;
    public UnityEngine.UI.Toggle speakToggle;

    // Example dictionary
    private Dictionary<string, HanziCategory> categories;

    private Dictionary<string, string> languages = new Dictionary<string, string>
    {
        { "en", "English" },
        { "de", "Deutsch" },
        { "fr", "Français" },
        { "it", "Italiano" },
        { "es", "Español" },
        { "ja", "日本語" },
        { "ko", "한국어" },
        { "ru", "Русский" }
    };

    // Start is called before the first frame update
    void Start()
    {
        PlayerPrefs.DeleteAll();
        HanziCategoryDB.Initialize();

        categories = HanziCategoryDB.Categories;
        categoryDropdown.AddOptions(categories.Keys.ToList());

        int savedIndex;
        // Optionally load previously saved value from PlayerPrefs
        string languageKey = PlayerPrefs.GetString("language", "en");
        string savedLanguage = languages.FirstOrDefault(x => x.Key == languageKey).Value;
        string savedCategory = PlayerPrefs.GetString("category", "");
        int savedPinyin = PlayerPrefs.GetInt("pinyin", 1);
        int savedTranslation = PlayerPrefs.GetInt("translation", 1);
        int savedSpeak = PlayerPrefs.GetInt("speak", 1);
        PlayerPrefs.SetString("language", languageKey);
        PlayerPrefs.SetString("category", savedCategory);
        PlayerPrefs.SetInt("pinyin", savedPinyin);
        PlayerPrefs.SetInt("translation", savedTranslation);
        PlayerPrefs.SetInt("speak", savedSpeak);


        // Set the dropdown value based on saved value
        savedIndex = languageDropdown.options.FindIndex(option => option.text == savedLanguage);
        if (savedIndex != -1)
        {
            languageDropdown.value = savedIndex;
        }
        savedIndex = categoryDropdown.options.FindIndex(option => option.text == savedCategory);
        if (savedIndex != -1)
        {
            categoryDropdown.value = savedIndex;
        }
        pinyinToggle.isOn = savedPinyin == 1;
        translationToggle.isOn = savedTranslation == 1;
        speakToggle.isOn = savedSpeak == 1;

        // Add listener for value change
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        pinyinToggle.onValueChanged.AddListener(OnPinyinChanged);
        translationToggle.onValueChanged.AddListener(OnTranslationChanged);
        speakToggle.onValueChanged.AddListener(OnSpeakChanged);
    }

    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }

    public void OnLanguageChanged(int index)
    {
        // Get the selected value as a string
        string selectedValue = languages.FirstOrDefault(x => x.Value == languageDropdown.options[index].text).Key;

        // Save the selected value to PlayerPrefs
        PlayerPrefs.SetString("language", selectedValue);
        PlayerPrefs.Save(); // Ensure changes are saved to disk
        foreach (Translator translator in Resources.FindObjectsOfTypeAll(typeof(Translator)) as Translator[])
        {
            translator.UpdateTranslation();
        }
    }

    public void OnCategoryChanged(int index)
    {
        // Get the selected value as a string
        string selectedValue = HanziCategoryDB.Categories.FirstOrDefault(x => x.Key == categoryDropdown.options[index].text).Value.hanzi;

        // Save the selected value to PlayerPrefs
        PlayerPrefs.SetString("hanzifilter", selectedValue);
        PlayerPrefs.SetString("category", categoryDropdown.options[index].text);
        PlayerPrefs.Save(); // Ensure changes are saved to disk
    }

    public void OnPinyinChanged(bool value)
    {
        int actualValue = pinyinToggle.isOn ? 1 : 0;
        PlayerPrefs.SetInt("pinyin", actualValue);
        PlayerPrefs.Save(); // Ensure changes are saved to disk
    }

    public void OnTranslationChanged(bool value)
    {
        int actualValue = translationToggle.isOn ? 1 : 0;
        PlayerPrefs.SetInt("translation", actualValue);
        PlayerPrefs.Save(); // Ensure changes are saved to disk
    }

    public void OnSpeakChanged(bool value)
    {
        int actualValue = speakToggle.isOn ? 1 : 0;
        PlayerPrefs.SetInt("speak", actualValue);
        PlayerPrefs.Save(); // Ensure changes are saved to disk

        UnityEngine.Debug.Log("Selected speak Value: " + actualValue); // Optional debug log
    }
}