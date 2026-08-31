using System.Collections;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerThoughts : MonoBehaviour
{
    public TMP_Text thoughtText;

    // Событие — вызовется когда все реплики закончатся
    public event System.Action OnThoughtsFinished;

    private string[] lines = new string[]
    {
        "Как болит голова...",
        "Боже, что со мной?",
        "Что это за шрам?",
        "Где я..."
    };

    public void StartThoughts()
    {
        Debug.Log("StartThoughts вызван");
        StartCoroutine(ShowLines());
    }

    IEnumerator ShowLines()
    {
        thoughtText.gameObject.SetActive(true);
        thoughtText.color = new Color(1f, 1f, 1f, 1f);

        foreach (string line in lines)
        {
            thoughtText.text = "";
            foreach (char c in line)
            {
                thoughtText.text += c;
                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(2f);

            yield return StartCoroutine(FadeOut());
            thoughtText.text = "";
        }

        thoughtText.gameObject.SetActive(false);

        // Реплики закончились — вызываем событие
        OnThoughtsFinished?.Invoke();
    }

    IEnumerator FadeOut()
    {
        Color c = thoughtText.color;
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            thoughtText.color = new Color(c.r, c.g, c.b, 1f - elapsed / 0.5f);
            yield return null;
        }
        thoughtText.color = new Color(c.r, c.g, c.b, 1f);
    }
}