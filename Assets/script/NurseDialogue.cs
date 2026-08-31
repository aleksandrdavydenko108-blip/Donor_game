using System.Collections;
using UnityEngine;
using TMPro;

public class NurseDialogue : MonoBehaviour
{
    public GameObject dialogueBox;
    public GameObject speakerPortrait;
    public TMP_Text dialogueText;
    public float typingSpeed = 0.04f;

    private string[] lines = new string[]
    {
       "Доброе утро! Вы живы. Это... приятно.",
       "Вам очень повезло. Очень. Фонд обычно выбирает только... подходящих кандидатов.",
       "Один зарубежный фонд оплатил всё. Они занимаются... благотворительностью. Да. Именно благотворительностью.",
       "Вам пересадили сердце, печень, почку и небольшой фрагмент мозговой ткани. Ничего особенного. Совершенно стандартная процедура.",
       "Донор был... очень здоровым человеком. Крепким. Целеустремлённым. Хватит об этом.",
       "Вы можете почувствовать небольшие изменения в характере. Это нормально. Абсолютно нормально. Не обращайте внимания.",
       "Осталась формальность — подписать согласие. Да, операция уже прошла. Но фонд любит порядок в документах. Мы тоже.",
       "Подпишите здесь. И здесь. Не читайте, там просто юридический текст. Очень скучный."
    };

    public event System.Action OnDialogueFinished;

    public void StartDialogue()
    {
        dialogueBox.SetActive(true);
        speakerPortrait.SetActive(true);
        StartCoroutine(PlayLines());
    }

    IEnumerator PlayLines()
    {
        foreach (string line in lines)
        {
            dialogueText.text = "";
            foreach (char c in line)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
            yield return new WaitForSeconds(2.5f);
        }

        dialogueText.text = "";
        dialogueBox.SetActive(false);
        speakerPortrait.SetActive(false);
        OnDialogueFinished?.Invoke();
    }
}