using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SplashText : MonoBehaviour
{
    public TextAsset textFile; // Указываем текстовый файл непосредственно в Inspector
    public TMP_Text splashText; // Ссылка на UI Text компонент
    private List<string> phrases = new List<string>();

    void Start()
    {
        LoadPhrases();
        SetRandomPhrase();
        StartCoroutine(PulsateText());
    }

    void LoadPhrases()
    {
        if (textFile != null)
        {
            // Чтение всех строк из файла
            string[] lines = textFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    phrases.Add(line.Trim());
                }
            }
        }
        else
        {
            Debug.LogError("Файл с фразами не найден!");
        }
    }

    void SetRandomPhrase()
    {
        if (phrases.Count > 0)
        {
            int randomIndex = Random.Range(0, phrases.Count);
            splashText.text = phrases[randomIndex];
        }
        else
        {
            splashText.text = "No phrases found!";
        }
    }

    IEnumerator PulsateText()
    {
        float pulsateSpeed = 6.0f;
        Vector3 originalScale = splashText.transform.localScale;
        while (true)
        {
            float scale = 1.0f + Mathf.Abs(Mathf.Sin(Time.time * pulsateSpeed)) * 0.1f; // Используем полуволну синуса
            splashText.transform.localScale = originalScale / scale;
            yield return null;
        }
    }
}
