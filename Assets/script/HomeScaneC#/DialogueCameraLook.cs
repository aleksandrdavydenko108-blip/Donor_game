using UnityEngine;

public class DialogueCameraLook : MonoBehaviour
{
    [Header("Настройки обзора")]
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -40f;
    [SerializeField] private float maxVerticalAngle = 40f;
    [SerializeField] private float minHorizontalAngle = -60f;
    [SerializeField] private float maxHorizontalAngle = 60f;

    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private Quaternion initialRotation;

    private bool controlsEnabled = false;

    private void Awake()
    {
        initialRotation = transform.localRotation;
    }

    public void EnableLook()
    {
        controlsEnabled = true;
        currentYaw = 0f;
        currentPitch = 0f;
        transform.localRotation = initialRotation;
    }

    public void DisableLook()
    {
        controlsEnabled = false;
    }

    private void Update()
    {
        if (!controlsEnabled) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        currentYaw += mouseX;
        currentPitch -= mouseY;

        currentYaw = Mathf.Clamp(currentYaw, minHorizontalAngle, maxHorizontalAngle);
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = initialRotation * Quaternion.Euler(currentPitch, currentYaw, 0f);
    }
}