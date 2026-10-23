using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Расставляет звуки в сценах автоматически при загрузке: фон и шаги.
// Добавлять на сцену не нужно.
public static class SceneAudioSetup
{
    private static readonly string[] WoodSteps = { "footstep_wood_000", "footstep_wood_001", "footstep_wood_002", "footstep_wood_003", "footstep_wood_004" };
    private static readonly string[] ConcreteSteps = { "footstep_concrete_000", "footstep_concrete_001", "footstep_concrete_002", "footstep_concrete_003", "footstep_concrete_004" };

    private const string RootName = "SceneAudio";

    // SubsystemRegistration срабатывает и при выключенном Domain Reload,
    // поэтому подписка не задваивается
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // При выключенном Reload Scene стартовая сцена не «загружается»
    // и sceneLoaded для неё не приходит — настраиваем её здесь
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SetupStartScene()
    {
        Setup(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Setup(scene);
    }

    private static void Setup(Scene scene)
    {
        // Звук мог быть приглушён затемнением в прошлой сцене
        AudioListener.volume = 1f;

        // Защита от повторной настройки одной и той же сцены
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == RootName) return;

        var root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        switch (scene.name)
        {
            case "Palata":
                CreateLoop(root, "hospital_ambience", 0.35f);
                CreateLoop(root, "monitor_beep", 0.25f);

                foreach (var nurse in Object.FindObjectsOfType<NurseController>(true))
                    AddFootsteps(nurse.gameObject, ConcreteSteps, 0.6f, true);
                break;

            case "HomeScene":
                CreateLoop(root, "home_ambience", 0.3f);

                foreach (var player in Object.FindObjectsOfType<FirstPersonMovementController>(true))
                    AddFootsteps(player.gameObject, WoodSteps, 0.35f, false);

                // Шаги мамы: рядом громко, к 10 метрам стихают совсем
                foreach (var mom in Object.FindObjectsOfType<MomWalkToLaundry>(true))
                {
                    var steps = AddFootsteps(mom.gameObject, WoodSteps, 0.6f, true);
                    if (steps != null)
                    {
                        steps.fullVolumeDistance = 1f;
                        steps.silentDistance = 10f;
                    }
                }
                break;
        }
    }

    private static Footsteps AddFootsteps(GameObject target, string[] sounds, float volume, bool spatial)
    {
        if (target.GetComponent<Footsteps>() != null) return null;

        var steps = target.AddComponent<Footsteps>();
        steps.stepSounds = sounds;
        steps.volume = volume;
        steps.spatial = spatial;
        return steps;
    }

    private static void CreateLoop(GameObject root, string clipName, float volume)
    {
        AudioClip clip = GameAudio.Load(clipName);
        if (clip == null) return;

        var go = new GameObject(clipName);
        go.transform.SetParent(root.transform, false);
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0f;
        src.Play();

        go.AddComponent<AudioFadeIn>().Begin(src, volume, 2f);
    }

    // Затухание звука вместе с картинкой (при переходе в другую сцену)
    public static IEnumerator FadeOutAll(float duration)
    {
        float start = AudioListener.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            AudioListener.volume = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }

        AudioListener.volume = 0f;
    }
}
