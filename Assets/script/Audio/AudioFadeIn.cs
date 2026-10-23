using System.Collections;
using UnityEngine;

// Плавно поднимает громкость источника от нуля
public class AudioFadeIn : MonoBehaviour
{
    public void Begin(AudioSource source, float targetVolume, float duration)
    {
        StartCoroutine(Fade(source, targetVolume, duration));
    }

    private IEnumerator Fade(AudioSource source, float targetVolume, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }
        source.volume = targetVolume;
    }
}
