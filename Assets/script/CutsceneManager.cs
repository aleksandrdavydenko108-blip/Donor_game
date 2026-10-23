using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    public string wardSceneName = "WardScene";
    public float cutsceneDuration = 10f;
    public float skipFadeDuration = 0.8f;

    private bool leaving = false;

    void Start()
    {
        SkipHint.Instance.ShowCutsceneHint();
        SkipHint.Instance.OnHoldComplete += Skip;
        StartCoroutine(PlayCutsceneAndLoad());
    }

    void OnDestroy()
    {
        if (SkipHint.HasInstance) SkipHint.Instance.OnHoldComplete -= Skip;
    }

    IEnumerator PlayCutsceneAndLoad()
    {
        yield return new WaitForSeconds(cutsceneDuration);
        Load();
    }

    // Удержание Пробела: плавно гасим картинку и звук, затем переходим дальше
    void Skip()
    {
        if (leaving) return;
        leaving = true;

        StopAllCoroutines();
        StartCoroutine(SkipRoutine());
    }

    IEnumerator SkipRoutine()
    {
        SkipHint.Instance.Hide();
        StartCoroutine(SceneAudioSetup.FadeOutAll(skipFadeDuration));
        yield return SkipHint.Instance.FadeToBlack(skipFadeDuration);
        SceneManager.LoadScene(wardSceneName);
    }

    void Load()
    {
        if (leaving) return;
        leaving = true;

        SkipHint.Instance.Hide();
        SceneManager.LoadScene(wardSceneName);
    }
}
