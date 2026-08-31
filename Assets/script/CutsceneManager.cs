using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    public string wardSceneName = "WardScene";
    public float cutsceneDuration = 10f;

    void Start()
    {
        StartCoroutine(PlayCutsceneAndLoad());
    }

    IEnumerator PlayCutsceneAndLoad()
    {
        yield return new WaitForSeconds(cutsceneDuration);
        SceneManager.LoadScene(wardSceneName);
    }
}