using System.Collections;
using TMPro;
using UnityEngine;

// Пропуск диалогов: нажатие Пробела/ЛКМ допечатывает реплику или переходит к следующей,
// удержание Пробела проматывает весь диалог.
public static class DialogueSkip
{
    private static int sessions = 0;

    public static bool SkipAll { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sessions = 0;
        SkipAll = false;
    }

    // Вызывать в начале диалога — показывает подсказку
    public static void Begin()
    {
        if (sessions == 0)
        {
            SkipAll = false;
            SkipHint.Instance.OnHoldComplete -= HandleHold;
            SkipHint.Instance.OnHoldComplete += HandleHold;
            SkipHint.Instance.ShowDialogueHint();
        }
        sessions++;
    }

    // Вызывать в конце диалога
    public static void End()
    {
        if (sessions == 0) return;

        sessions--;
        if (sessions == 0)
        {
            SkipAll = false;
            SkipHint.Instance.OnHoldComplete -= HandleHold;
            SkipHint.Instance.Hide();
        }
    }

    private static void HandleHold()
    {
        if (sessions > 0) SkipAll = true;
    }

    // Печать текста по буквам; по нажатию сразу показывает всю строку
    public static IEnumerator Type(TMP_Text text, string line, float delay, float typingPitch = 1f)
    {
        text.text = "";

        foreach (char c in line)
        {
            text.text += c;
            GameAudio.PlayTyping(c, typingPitch);

            float end = Time.time + delay;
            do
            {
                yield return null;
                if (SkipAll || SkipHint.TapDown)
                {
                    text.text = line;
                    yield break;
                }
            }
            while (Time.time < end);
        }
    }

    // Пауза после реплики; нажатие её обрывает
    public static IEnumerator WaitOrSkip(float seconds)
    {
        float end = Time.time + seconds;
        do
        {
            yield return null;
            if (SkipAll || SkipHint.TapDown) yield break;
        }
        while (Time.time < end);
    }
}
