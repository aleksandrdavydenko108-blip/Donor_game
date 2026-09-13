using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public class DialogueLine
    {
        public string text;
        public Speaker speaker;

        public DialogueLine(Speaker speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }

    public enum Speaker { Mom, GG }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private string momDisplayName = "Мама";
    [SerializeField] private string ggDisplayName = "ГГ";

    [Header("Портреты")]
    [SerializeField] private Sprite momPortrait;
    [SerializeField] private Sprite ggPortrait;

    [Header("Аниматоры")]
    [SerializeField] private Animator momAnimator;
    [SerializeField] private Animator ggAnimator;
    [SerializeField] private string talkingBoolParam = "IsTalking";
    [SerializeField] private MomStandRepositioner momRepositioner;

    [Header("Печать текста")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float pauseAfterLine = 1.5f;

    public Action OnDialogueStarted;
    public Action OnDialogueEnded;

    private List<DialogueLine> lines = new List<DialogueLine>();
    private int currentLineIndex = -1;
    private bool dialogueActive = false;
    private Coroutine typingCoroutine;

    public bool IsDialogueActive => dialogueActive;

    private void Awake()
    {
        BuildLines();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (speakerNameText != null) speakerNameText.text = "";
        if (dialogueText != null) dialogueText.text = "";
        if (portraitImage != null) portraitImage.enabled = false;
    }

    private void BuildLines()
    {
        lines.Add(new DialogueLine(Speaker.Mom, "Ты сегодня выглядишь получше. Как самочувствие?"));
        lines.Add(new DialogueLine(Speaker.GG, "Нормально. Голова иногда болит, но врач говорил, что это пройдет."));

        lines.Add(new DialogueLine(Speaker.Mom, "Три месяца в больнице... Я каждый день боялась, что мне позвонят и скажут что-то плохое."));
        lines.Add(new DialogueLine(Speaker.GG, "Прости, что заставил тебя так переживать."));

        lines.Add(new DialogueLine(Speaker.Mom, "Не извиняйся. Главное, что ты жив. А вот Сэма... его не спасли."));
        lines.Add(new DialogueLine(Speaker.GG, "Я знаю. Думаю об этом каждый день."));

        lines.Add(new DialogueLine(Speaker.Mom, "Вы столько лет дружили. Мне до сих пор не верится, что его больше нет."));
        lines.Add(new DialogueLine(Speaker.GG, "Мне тоже. Иногда кажется, что он просто не отвечает на звонки, а не..."));

        lines.Add(new DialogueLine(Speaker.Mom, "Ты с ним разговаривал перед аварией? Помнишь, о чём говорили?"));
        lines.Add(new DialogueLine(Speaker.GG, "Помню. Ничего особенного. Обычная ерунда, как всегда."));

        lines.Add(new DialogueLine(Speaker.Mom, "Значит, повезло, что успели хоть о чём-то поговорить."));
        lines.Add(new DialogueLine(Speaker.GG, "Наверное."));

        lines.Add(new DialogueLine(Speaker.Mom, "Ладно, не буду больше об этом. Слушай, ты не мог бы порезать мясо на кухне? Я устала, руки уже не те."));
        lines.Add(new DialogueLine(Speaker.GG, "Хорошо, сделаю."));
    }

    public void StartDialogue()
    {
        if (lines.Count == 0)
        {
            Debug.LogWarning("DialogueManager: список реплик пуст.");
            return;
        }

        dialogueActive = true;
        currentLineIndex = -1;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        OnDialogueStarted?.Invoke();

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = lines[currentLineIndex];

        bool isMom = line.speaker == Speaker.Mom;

        speakerNameText.text = isMom ? momDisplayName : ggDisplayName;

        if (portraitImage != null)
        {
            portraitImage.sprite = isMom ? momPortrait : ggPortrait;
            portraitImage.enabled = true;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLineThenAdvance(line.text));
    }

    private IEnumerator TypeLineThenAdvance(string fullText)
    {
        dialogueText.text = "";

        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(pauseAfterLine);

        ShowNextLine();
    }

    private void EndDialogue()
    {
        dialogueActive = false;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (portraitImage != null) portraitImage.enabled = false;
        if (speakerNameText != null) speakerNameText.text = "";
        if (dialogueText != null) dialogueText.text = "";

        if (ggAnimator != null)
        {
            ggAnimator.applyRootMotion = true;
            ggAnimator.SetTrigger("StandUp");
        }

        if (momAnimator != null)
        {
            momAnimator.SetTrigger("StandUp");
        }

        if (momRepositioner != null)
        {
            momRepositioner.PlayReposition();
        }

        OnDialogueEnded?.Invoke();
    }

    public void ShowSingleLine(Speaker speaker, string text, float duration)
    {
        if (dialogueActive) return;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        bool isMom = speaker == Speaker.Mom;
        speakerNameText.text = isMom ? momDisplayName : ggDisplayName;

        if (portraitImage != null)
        {
            portraitImage.sprite = isMom ? momPortrait : ggPortrait;
            portraitImage.enabled = true;
        }

        if (isMom && momAnimator != null)
        {
            momAnimator.SetTrigger("ShortTalk");
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeSingleLineThenHide(text, duration));
    }

    private IEnumerator TypeSingleLineThenHide(string fullText, float duration)
    {
        dialogueText.text = "";

        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(duration);

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (portraitImage != null) portraitImage.enabled = false;
        speakerNameText.text = "";
        dialogueText.text = "";
    }
}