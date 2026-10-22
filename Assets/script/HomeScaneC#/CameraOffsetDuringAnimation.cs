using System.Collections;
using UnityEngine;

public class CameraOffsetDuringAnimation : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 pulledBackLocalPosition;
    [SerializeField] private float transitionDuration = 0.2f;

    private Vector3 originalLocalPosition;
    private bool initialized = false;

    private void EnsureInit()
    {
        if (!initialized && cameraTransform != null)
        {
            originalLocalPosition = cameraTransform.localPosition;
            initialized = true;
        }
    }

    // Вызывается Animation Event в начале анимации TakeKnife
    public void PullCameraBack()
    {
        EnsureInit();
        StopAllCoroutines();
        StartCoroutine(LerpCamera(pulledBackLocalPosition));
    }

    // Вызывается Animation Event ближе к концу анимации TakeKnife
    public void RestoreCameraPosition()
    {
        EnsureInit();
        StopAllCoroutines();
        StartCoroutine(LerpCamera(originalLocalPosition));
    }

    private IEnumerator LerpCamera(Vector3 target)
    {
        Vector3 start = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            cameraTransform.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }

        cameraTransform.localPosition = target;
    }
}