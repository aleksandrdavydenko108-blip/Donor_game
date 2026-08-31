using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections; // Добавил для работы затухания

public class MainMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI и Сцены")]
    public CanvasGroup menuCanvasGroup;
    public Image backgroundDisplay;
    public string gameSceneName = "Cutscene";

    [Header("Спрайты")]
    public Sprite normalSprite;
    public Sprite hoverSprite;

    [Header("Звуки")]
    public AudioSource sfxSource;
    public AudioClip hoverSound;
    public AudioSource menuMusic;

    // 1. Метод для запуска игры (с затуханием)
    public void PlayGame()
    {
        StartCoroutine(FadeOutAndStart());
    }

    // 2. Метод для выхода
    public void QuitGame()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
    }

    // 3. Корутина затухания
    IEnumerator FadeOutAndStart()
    {
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        float duration = 0.8f;
        float currentTime = 0;
        float startVol = (menuMusic != null) ? menuMusic.volume : 0;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / duration;

            if (menuCanvasGroup != null) menuCanvasGroup.alpha = 1 - progress;
            if (menuMusic != null) menuMusic.volume = Mathf.Lerp(startVol, 0, progress);

            yield return null;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    // 4. При наведении мышки
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Меняем фон
        if (backgroundDisplay != null && hoverSprite != null)
        {
            backgroundDisplay.sprite = hoverSprite;
        }

        // Играем звук (исправил проверку на != null)
        if (sfxSource != null && hoverSound != null)
        {
            sfxSource.PlayOneShot(hoverSound);
        }
    }

    // 5. Когда мышка уходит
    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundDisplay != null && normalSprite != null)
        {
            backgroundDisplay.sprite = normalSprite;
        }
    }
} // ВНИМАНИЕ: Это последняя скобка, она закрывает весь класс!