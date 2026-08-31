using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeInOnStart : MonoBehaviour
{
    [Header("Настройки затухания")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float delayBeforeFade = 0.5f;

    public Action OnFadeComplete;

    private Image fadeImage;

    private void Awake()
    {
        fadeImage = GetComponent<Image>();

        if (fadeImage == null)
        {
            Debug.LogError("FadeInOnStart: на объекте нет компонента Image!");
            return;
        }

        Color startColor = fadeImage.color;
        startColor.a = 1f;
        fadeImage.color = startColor;
        fadeImage.raycastTarget = true;
    }

    private void Start()
    {
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        if (delayBeforeFade > 0f)
            yield return new WaitForSeconds(delayBeforeFade);

        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            color.a = Mathf.Lerp(1f, 0f, t);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;

        fadeImage.raycastTarget = false;
        gameObject.SetActive(false);

        OnFadeComplete?.Invoke();
    }
}