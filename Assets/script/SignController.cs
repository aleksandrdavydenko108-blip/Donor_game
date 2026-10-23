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
            StartCoroutine(PlaySignatureSounds());
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
        DialogueSkip.Begin();
        yield return DialogueSkip.Type(dialogueText, line, 0.04f);
        yield return DialogueSkip.WaitOrSkip(2f);
        DialogueSkip.End();

        dialogueBox.SetActive(false);
        speakerPortrait.SetActive(false);

        // Возвращаем портрет медсестры на случай если нужно
        speakerImage.sprite = nursePortrait;

        StartCoroutine(SceneAudioSetup.FadeOutAll(fadeDuration));
        yield return StartCoroutine(FadeToBlack());

        SceneManager.LoadScene(nextScene);
    }

    IEnumerator PlaySignatureSounds()
    {
        GameAudio.Play("bookFlip2", 0.5f);
        yield return new WaitForSeconds(0.4f);

        string[] scratches = { "scratch_001", "scratch_002", "scratch_003", "scratch_002" };
        foreach (string s in scratches)
        {
            GameAudio.Play(s, 0.45f, Random.Range(0.9f, 1.1f));
            yield return new WaitForSeconds(Random.Range(0.12f, 0.22f));
        }
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