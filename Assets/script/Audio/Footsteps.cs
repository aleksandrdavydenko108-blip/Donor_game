using UnityEngine;

// Шаги по пройденному расстоянию: каждые strideLength метров играет звук шага.
// Работает и для игрока, и для NPC, анимационные события не нужны.
public class Footsteps : MonoBehaviour
{
    public string[] stepSounds = { "footstep_wood_000", "footstep_wood_001", "footstep_wood_002", "footstep_wood_003", "footstep_wood_004" };
    public float strideLength = 0.65f;
    public float volume = 0.5f;
    public bool spatial = true;

    [Header("Затухание с расстоянием (для spatial)")]
    public float fullVolumeDistance = 1f;
    public float silentDistance = 15f;

    private Vector3 lastPosition;
    private float distance = 0f;
    private int lastIndex = -1;

    private void OnEnable()
    {
        lastPosition = transform.position;
        distance = 0f;
    }

    private void Update()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        delta.y = 0f;

        float moved = delta.magnitude;

        // Телепорты (пересадка игрока скриптом) за шаги не считаем
        if (moved > 1f || moved < 0.0001f)
        {
            if (moved > 1f) distance = 0f;
            return;
        }

        distance += moved;
        if (distance < strideLength) return;

        distance = 0f;
        PlayStep();
    }

    private void PlayStep()
    {
        if (stepSounds == null || stepSounds.Length == 0) return;

        int index = Random.Range(0, stepSounds.Length);
        if (stepSounds.Length > 1 && index == lastIndex) index = (index + 1) % stepSounds.Length;
        lastIndex = index;

        AudioClip clip = GameAudio.Load(stepSounds[index]);
        float pitch = Random.Range(0.92f, 1.08f);

        if (spatial)
            GameAudio.PlayAt(clip, transform.position, volume, pitch, fullVolumeDistance, silentDistance);
        else
            GameAudio.Play(clip, volume, pitch);
    }
}
