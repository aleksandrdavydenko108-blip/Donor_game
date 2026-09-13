using System;
using System.Collections;
using UnityEngine;

public class MomWalkToLaundry : MonoBehaviour
{
    [Header("Куда идти")]
    [SerializeField] private Transform[] waypoints;

    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    public bool HasArrived { get; private set; } = false;
    public Action OnArrived;

    public void StartWalking()
    {
        if (animator != null)
            animator.applyRootMotion = false;

        StartCoroutine(WalkRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        foreach (Transform point in waypoints)
        {
            yield return MoveToPoint(point.position);
        }

        Vector3 finalEuler = transform.eulerAngles;
        finalEuler.y = 0f;
        transform.eulerAngles = finalEuler;

        if (animator != null)
            animator.SetFloat(speedParam, 0f);

        HasArrived = true;
        OnArrived?.Invoke();
    }

    private IEnumerator MoveToPoint(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > arrivalThreshold)
        {
            Vector3 direction = (target - transform.position);
            direction.y = 0f;
            direction.Normalize();

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(target.x, transform.position.y, target.z),
                moveSpeed * Time.deltaTime
            );

            if (animator != null)
                animator.SetFloat(speedParam, 1f);

            yield return null;
        }
    }
}