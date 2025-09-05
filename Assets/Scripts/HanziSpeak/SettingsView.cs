using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsView : AppView
{
    public List<GameObject> startButtons;
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown voiceDropdown;
    public UnityEngine.UI.Toggle pinyinToggle;
    public UnityEngine.UI.Toggle translationToggle;
    public UnityEngine.UI.Toggle speakToggle;

    // Example dictionary
    private Dictionary<string, HanziTranslations> categories;
    private Dictionary<string, HanziTranslations> voices;
    private string needsPracticeIndex;
    private List<List<int>> levelCounts;

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
        HanziLevelDB.Initialize();
        HanziVoiceDB.Initialize();
        TranslationDB.Initialize();

        UpdateCategories();
        UpdateVoices();

        int savedIndex;
        // Optionally load previously saved value from PlayerPrefs
        string languageKey = PlayerPrefs.GetString("language", "en");
        string savedLanguage = languages.FirstOrDefault(x => x.Key == languageKey).Value;
        int savedVoice = PlayerPrefs.GetInt("voice", 0);
        int savedCategory = PlayerPrefs.GetInt("category", 0);
        int savedPinyin = PlayerPrefs.GetInt("pinyin", 1);
        int savedTranslation = PlayerPrefs.GetInt("translation", 1);
        int savedSpeak = PlayerPrefs.GetInt("speak", 1);
        PlayerPrefs.SetString("hanzifilter", string.Empty);
        PlayerPrefs.SetString("language", languageKey);
        PlayerPrefs.SetInt("voice", savedVoice < voiceDropdown.options.Count() ? savedVoice : 0);
        PlayerPrefs.SetInt("category", savedCategory < categoryDropdown.options.Count() ? savedCategory : 0);
        PlayerPrefs.SetInt("pinyin", savedPinyin);
        PlayerPrefs.SetInt("translation", savedTranslation);
        PlayerPrefs.SetInt("speak", savedSpeak);
        OnCategoryChanged(savedCategory);
        OnVoiceChanged(savedVoice);
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
        voiceDropdown.onValueChanged.AddListener(OnVoiceChanged);
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
        UpdateVoices();
    }

    public void UpdateCategories()
    {
        categoryDropdown.options.Clear();
        categories = HanziCategoryDB.GetAllCategories();
        AddNeedsPracticeOption(PlayerPrefs.GetString("failedHanzi"));
        categoryDropdown.AddOptions(HanziCategoryDB.GetAllKeys().ToList());
        foreach (Translator translator in Resources.FindObjectsOfTypeAll(typeof(Translator)) as Translator[])
        {
            translator.UpdateTranslation();
        }
    }

    public void UpdateVoices()
    {
        voiceDropdown.options.Clear();
        voices = HanziVoiceDB.GetAllVoices();
        voiceDropdown.AddOptions(HanziVoiceDB.GetAllKeys().ToList());
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
            if (needsPracticeIndex != null && HanziCategoryDB.GetAllCategories().ContainsKey(needsPracticeIndex))
            {
                HanziCategoryDB.GetAllCategories().Remove(needsPracticeIndex);
            }
            return;
        }
        if (needsPracticeIndex != null && HanziCategoryDB.GetAllCategories().ContainsKey(needsPracticeIndex))
        {
            return;
        }
        HanziTranslations needsPractice = new HanziTranslations();
        string translationKey = "needsPractice";
        Translation needsPracticeTranslation = TranslationDB.GetTranslations(translationKey);
        needsPractice.de = needsPracticeTranslation.de;
        needsPractice.en = needsPracticeTranslation.en;
        needsPractice.fr = needsPracticeTranslation.fr;
        needsPractice.it = needsPracticeTranslation.it;
        needsPractice.es = needsPracticeTranslation.es;
        needsPractice.ja = needsPracticeTranslation.ja;
        needsPractice.ko = needsPracticeTranslation.ko;
        needsPractice.ru = needsPracticeTranslation.ru;
        needsPracticeIndex = HanziCategoryDB.GetAllCategories().Count.ToString();
        HanziCategoryDB.GetAllCategories().Add(needsPracticeIndex, needsPractice);
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
        string hanziCategory = index < HanziCategoryDB.GetAllKeys().Count()
            ? HanziCategoryDB.GetAllKeys().ElementAt(index)
            : "Needs Practice";

        PlayerPrefs.SetInt("needsPractice", 0);
        if (hanziCategory == "Needs Practice")
        {
            PlayerPrefs.SetInt("needsPractice", 1);
        }

        HandleStartButtons(hanziCategory);
        // Save the selected value to PlayerPrefs
        PlayerPrefs.SetInt("category", index);
        PlayerPrefs.Save(); // Ensure changes are saved to disk
    }

    public void HandleStartButtons(string hanziCategory)
    {
        List<int> levelCounts = new List<int>();

        for (int index = 0; index < HanziLevelDB.GetAllKeys().Count(); index++)
        {
            string level = HanziLevelDB.GetAllKeys().ElementAt(index);
            List<string> hanziList = HanziLevelDB.FilterHanzi(hanziCategory, level);
            GameObject button = startButtons[index];
            if (hanziList.Count > 0)
            {
                levelCounts.Add(hanziList.Count);
                string levelKey = button.name;
                // Show the button
                button.SetActive(true);

                // Remove existing listeners to avoid duplicates
                Button btn = button.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();

                int levelCopy = index; // Avoid closure issue

                // Bind level to button click
                btn.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetInt("level", levelCopy);
                    PlayerPrefs.Save();
                    AppManager.Instance.PlayView();
                });
            }
            else if (button)
            {
                // Hide unused buttons
                button.SetActive(false);
            }
        }
    }

    public void OnVoiceChanged(int index)
    {
        PlayerPrefs.SetInt("voice", index);
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