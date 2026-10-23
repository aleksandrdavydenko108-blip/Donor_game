using UnityEngine;

// Убирает покачивание камеры, прикреплённой к кости головы.
// Камера следует за головой (вставание, наклоны), но через сглаживание,
// которое гасит быстрые колебания от шагов. localPosition/localRotation камеры
// по-прежнему задают другие скрипты (обзор мышью, отъезд камеры при анимации).
[DefaultExecutionOrder(-1000)]
public class CameraHeadBobStabilizer : MonoBehaviour
{
    [SerializeField] private Transform body;

    [Header("Сглаживание (больше = меньше покачивания, но больше запаздывание)")]
    [SerializeField, Range(0f, 0.5f)] private float positionSmoothTime = 0.15f;
    [SerializeField, Range(0f, 0.5f)] private float rotationSmoothTime = 0.15f;

    private Transform head;

    // Поза головы в пространстве тела, сглаженная в два каскада
    private Vector3 positionStage1, positionStage2;
    private Quaternion rotationStage1, rotationStage2;
    private bool initialized = false;

    private Vector3 desiredLocalPosition;
    private Quaternion desiredLocalRotation;
    private bool applied = false;

    private void Awake()
    {
        head = transform.parent;

        if (body == null)
        {
            var movement = GetComponentInParent<FirstPersonMovementController>();
            body = movement != null ? movement.transform : transform.root;
        }
    }

    private void Update()
    {
        // Возвращаем локальную позу, чтобы остальные скрипты работали как раньше
        RestoreLocalPose();
    }

    private void LateUpdate()
    {
        if (head == null || body == null) return;

        Vector3 headPosition = body.InverseTransformPoint(head.position);
        Quaternion headRotation = Quaternion.Inverse(body.rotation) * head.rotation;

        if (!initialized)
        {
            positionStage1 = positionStage2 = headPosition;
            rotationStage1 = rotationStage2 = headRotation;
            initialized = true;
        }

        float pt = SmoothFactor(positionSmoothTime);
        positionStage1 = Vector3.Lerp(positionStage1, headPosition, pt);
        positionStage2 = Vector3.Lerp(positionStage2, positionStage1, pt);

        float rt = SmoothFactor(rotationSmoothTime);
        rotationStage1 = Quaternion.Slerp(rotationStage1, headRotation, rt);
        rotationStage2 = Quaternion.Slerp(rotationStage2, rotationStage1, rt);

        desiredLocalPosition = transform.localPosition;
        desiredLocalRotation = transform.localRotation;
        applied = true;

        Vector3 stablePosition = body.TransformPoint(positionStage2);
        Quaternion stableRotation = body.rotation * rotationStage2;

        transform.SetPositionAndRotation(
            stablePosition + stableRotation * Vector3.Scale(head.lossyScale, desiredLocalPosition),
            stableRotation * desiredLocalRotation);
    }

    private static float SmoothFactor(float smoothTime)
    {
        if (smoothTime <= 0f) return 1f;
        return 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
    }

    private void OnDisable()
    {
        RestoreLocalPose();
    }

    private void RestoreLocalPose()
    {
        if (!applied) return;

        transform.localPosition = desiredLocalPosition;
        transform.localRotation = desiredLocalRotation;
        applied = false;
    }
}
