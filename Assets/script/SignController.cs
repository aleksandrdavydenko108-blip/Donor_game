using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SignController : MonoBehaviour
{
    public GameObject signButton;
    public GameObject dialogueBox;
    public GameObject speakerPortrait;
    public TMP_Text dialogueText;
    public Image fadeImage;
    public float fadeDuration = 2f;
    public string nextScene = "HomeScene";

    public Sprite ggPortrait;
    public Sprite nursePortrait;
    private Image speakerImage;

    void Awake()
    {
        speakerImage = speakerPortrait.GetComponent<Image>();
    }

    public void ShowSignButton()
    {
        StartCoroutine(ShowAfterDelay());
    }

    IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        signButton.SetActive(true);
    }

    void Update()
    {
        if (signButton.activeSelf && Input.GetKeyDown(KeyCode.F))
        {
            signButton.SetActive(false);
            StartCoroutine(SignSequence());
        }
    }

    IEnumerator SignSequence()
    {
        // Меняем портрет на гг
        speakerImage.sprite = ggPortrait;

        dialogueBox.SetActive(true);
        speakerPortrait.SetActive(true);
        dialogueText.text = "";

        string line = "Хорошо... я подпишу.";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.04f);
        }

        yield return new WaitForSeconds(2f);

        dialogueBox.SetActive(false);
        speakerPortrait.SetActive(false);

        // Возвращаем портрет медсестры на случай если нужно
        speakerImage.sprite = nursePortrait;

        yield return StartCoroutine(FadeToBlack());

        SceneManager.LoadScene(nextScene);
    }

    IEnumerator FadeToBlack()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, elapsed / fadeDuration);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 1);
    }
}