using TMPro;
using UnityEngine;

public class Translator : MonoBehaviour
{
    [SerializeField] private string translationKey;
    [SerializeField] private TMP_Text textField;

    void Start()
    {
        TranslationDB.Initialize();
        textField = gameObject.GetComponent<TMP_Text>();
        UpdateTranslation();
    }
    public void UpdateTranslation()
    {
        if (textField != null) textField.text = TranslationDB.GetTranslations(translationKey).GetTranslationString();
    }
}