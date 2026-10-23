using System.Collections.Generic;
using UnityEngine;

// Простой проигрыватель звуков. Клипы лежат в Assets/Resources/Audio
// и загружаются по имени файла (без расширения).
public static class GameAudio
{
    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private static AudioSource[] sources2D;
    private static int nextSource = 0;
    private static float lastTypingTime = -1f;
    private static int typingCounter = 0;

    // При выключенном Domain Reload статические поля переживают перезапуск Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache.Clear();
        sources2D = null;
        nextSource = 0;
        lastTypingTime = -1f;
        typingCounter = 0;
    }

    public static AudioClip Load(string name)
    {
        if (cache.TryGetValue(name, out AudioClip clip)) return clip;

        clip = Resources.Load<AudioClip>("Audio/" + name);
        if (clip == null) Debug.LogWarning("GameAudio: нет звука Audio/" + name);
        cache[name] = clip;
        return clip;
    }

    // Случайный вариант из нескольких: names = {"cloth1", "cloth2"}
    public static AudioClip LoadRandom(params string[] names)
    {
        return Load(names[Random.Range(0, names.Length)]);
    }

    public static void Play(string name, float volume = 1f, float pitch = 1f)
    {
        Play(Load(name), volume, pitch);
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource src = Get2DSource();
        src.pitch = pitch;
        src.PlayOneShot(clip, volume);
    }

    // Кривая затухания: на minDistance полная громкость, к maxDistance плавно до нуля
    private static readonly AnimationCurve fadeCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -2f),
        new Keyframe(1f, 0f, 0f, 0f));

    public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f,
        float minDistance = 1f, float maxDistance = 15f)
    {
        if (clip == null) return;

        var go = new GameObject("OneShot_" + clip.name);
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 1f;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.rolloffMode = AudioRolloffMode.Custom;
        src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, fadeCurve);
        src.dopplerLevel = 0f;
        src.Play();
        Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
    }

    // Тихий «тик» при печати текста. Вызывать на каждую букву —
    // частоту сам ограничивает, пробелы пропускает.
    public static void PlayTyping(char c, float pitch = 1f)
    {
        if (char.IsWhiteSpace(c)) return;
        if (Time.unscaledTime - lastTypingTime < 0.06f) return;

        lastTypingTime = Time.unscaledTime;
        typingCounter++;

        string[] ticks = { "tick_001", "tick_002", "tick_004" };
        Play(ticks[typingCounter % ticks.Length], 0.18f, pitch * Random.Range(0.95f, 1.05f));
    }

    // Несколько источников по кругу, чтобы pitch одного звука не менял другие
    private static AudioSource Get2DSource()
    {
        if (sources2D == null || sources2D[0] == null)
        {
            var go = new GameObject("GameAudio2D");
            Object.DontDestroyOnLoad(go);

            sources2D = new AudioSource[8];
            for (int i = 0; i < sources2D.Length; i++)
            {
                sources2D[i] = go.AddComponent<AudioSource>();
                sources2D[i].playOnAwake = false;
                sources2D[i].spatialBlend = 0f;
            }
        }

        nextSource = (nextSource + 1) % sources2D.Length;
        return sources2D[nextSource];
    }
}
