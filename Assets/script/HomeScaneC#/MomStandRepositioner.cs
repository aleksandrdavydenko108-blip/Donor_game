using System.Collections;
using UnityEngine;

public class MomStandRepositioner : MonoBehaviour
{
    [Header("÷елевой угол поворота")]
    [SerializeField] private float targetRotY = 82.49f;

    [Header("ƒлительность поворота (должна совпадать с длиной анимации вставани€)")]
    [SerializeField] private float rotationDuration = 1.2f;

    private bool isRotating = false;
    private float rotElapsed = 0f;
    private Quaternion rotStart;
    private Quaternion rotTarget;

    public void PlayReposition()
    {
        rotStart = transform.rotation;

        Vector3 currentEuler = transform.eulerAngles;
        rotTarget = Quaternion.Euler(currentEuler.x, targetRotY, currentEuler.z);

        rotElapsed = 0f;
        isRotating = true;
    }

    private void LateUpdate()
    {
        if (!isRotating) return;

        rotElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(rotElapsed / rotationDuration);

        transform.rotation = Quaternion.Slerp(rotStart, rotTarget, t);

        if (t >= 1f)
        {
            isRotating = false;
        }
    }
}