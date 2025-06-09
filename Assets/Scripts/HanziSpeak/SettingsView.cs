using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Windows;

public class SettingsView : AppView
{
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown categoryDropdown;
    public UnityEngine.UI.Toggle pinyinToggle;
    public UnityEngine.UI.Toggle translationToggle;
    public UnityEngine.UI.Toggle speakToggle;

    // Example dictionary
    private Dictionary<string, HanziCategory> categories;
    private string needsPracticeIndex;

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
        HanziCategoryDB.Initialize();
        TranslationDB.Initialize();

        UpdateCategories();

        int savedIndex;
        // Optionally load previously saved value from PlayerPrefs
        string languageKey = PlayerPrefs.GetString("language", "en");
        string savedLanguage = languages.FirstOrDefault(x => x.Key == languageKey).Value;
        int savedCategory = PlayerPrefs.GetInt("category", 0);
        int savedPinyin = PlayerPrefs.GetInt("pinyin", 1);
        int savedTranslation = PlayerPrefs.GetInt("translation", 1);
        int savedSpeak = PlayerPrefs.GetInt("speak", 1);
        PlayerPrefs.SetString("language", languageKey);
        PlayerPrefs.SetInt("category", savedCategory);
        PlayerPrefs.SetInt("pinyin", savedPinyin);
        PlayerPrefs.SetInt("translation", savedTranslation);
        PlayerPrefs.SetInt("speak", savedSpeak);
        PlayerPrefs.Save();


        // Set the dropdown value based on saved value
        savedIndex = languageDropdown.options.FindIndex(option => option.text == savedLanguage);
        if (savedIndex != -1)
        {
            languageDropdown.value = savedIndex;
        }
        if (savedCategory != -1)
        {
            categoryDropdown.value = savedCategory;
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
        foreach (Translator translator in Resources.FindObjectsOfTypeAll(typeof(Translator)) as Translator[])
        {
            translator.UpdateTranslation();
        }
    }

    public void ShowView()
    {
        base.ShowView();
        UpdateCategories();
    }

    public void UpdateCategories()
    {
        categoryDropdown.options.Clear();
        categories = HanziCategoryDB.Categories;
        AddNeedsPracticeOption(PlayerPrefs.GetString("failedHanzi"));
        categoryDropdown.AddOptions(categories.Keys.ToList());
        foreach (Translator translator in Resources.FindObjectsOfTypeAll(typeof(Translator)) as Translator[])
        {
            translator.UpdateTranslation();
        }
    }

    private void AddNeedsPracticeOption(string failedChars)
    {
        UnityEngine.Debug.Log(failedChars);
        if (string.IsNullOrEmpty(failedChars)) {
            UnityEngine.Debug.Log($"needs practice index: {needsPracticeIndex}");
            if (needsPracticeIndex != null && HanziCategoryDB.Categories.ContainsKey(needsPracticeIndex))
            {
                HanziCategoryDB.Categories.Remove(needsPracticeIndex);
            }
            return;
        }
        if (needsPracticeIndex != null && HanziCategoryDB.Categories.ContainsKey(needsPracticeIndex))
        {
            return;
        }
        HanziCategory needsPractice = new HanziCategory();
        HanziTranslations translations = new HanziTranslations();
        string translationKey = "needsPractice";
        Translation needsPracticeTranslation = TranslationDB.GetTranslations(translationKey);
        translations.de = needsPracticeTranslation.de;
        translations.en = needsPracticeTranslation.en;
        translations.fr = needsPracticeTranslation.fr;
        translations.it = needsPracticeTranslation.it;
        translations.es = needsPracticeTranslation.es;
        translations.ja = needsPracticeTranslation.ja;
        translations.ko = needsPracticeTranslation.ko;
        translations.ru = needsPracticeTranslation.ru;
        needsPractice.hanzi = PlayerPrefs.GetString("failedHanzi");
        needsPractice.title = translations;
        needsPracticeIndex = HanziCategoryDB.Categories.Count.ToString();
        HanziCategoryDB.Categories.Add(needsPracticeIndex, needsPractice);
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
        HanziCategory hanziCategory = HanziCategoryDB.Categories.FirstOrDefault(x => x.Key == index.ToString()).Value;

        PlayerPrefs.SetInt("needsPractice", 0);
        if (hanziCategory.title.en == "Needs Practice")
        {
            PlayerPrefs.SetInt("needsPractice", 1);
            PlayerPrefs.SetString("hanzifilter", PlayerPrefs.GetString("failedHanzi"));
        }
        else
        {
            PlayerPrefs.SetString("hanzifilter", hanziCategory.hanzi);
        }
        // Save the selected value to PlayerPrefs
        PlayerPrefs.SetInt("category", index);
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