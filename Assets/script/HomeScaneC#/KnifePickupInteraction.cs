using System.Collections;
using UnityEngine;

public class KnifePickupInteraction : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Animator ggAnimator;
    [SerializeField] private MonoBehaviour firstPersonMovementScript;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private MeatHighlight meatHighlight;

    [Header("Точная позиция для анимации взятия ножа")]
    [SerializeField] private Vector3 targetPosition = new Vector3(18.12924f, -0.020001f, -21.92355f);
    [SerializeField] private float targetRotationY = 3.335f;

    [Header("Настройки")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private bool playerInRange = false;
    private bool knifeTaken = false;
    private Transform playerTransform;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;

            if (!knifeTaken)
            {
                if (interactPrompt != null)
                    interactPrompt.SetActive(true);

                if (meatHighlight != null)
                    meatHighlight.EnableHighlight();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (interactPrompt != null)
                interactPrompt.SetActive(false);

            if (meatHighlight != null)
                meatHighlight.DisableHighlight();
        }
    }

    private void Update()
    {
        if (!playerInRange || knifeTaken) return;

        if (Input.GetKeyDown(interactKey))
        {
            TakeKnife();
        }
    }

    private void TakeKnife()
    {
        knifeTaken = true;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        if (meatHighlight != null)
            meatHighlight.DisableHighlight();

        if (firstPersonMovementScript != null)
            firstPersonMovementScript.enabled = false;

        if (playerTransform != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = targetPosition;
            playerTransform.rotation = Quaternion.Euler(0f, targetRotationY, 0f);

            if (cc != null) cc.enabled = true;
        }

        if (ggAnimator != null)
        {
            ggAnimator.SetBool("HasKnife", true);
            ggAnimator.SetTrigger("TakeKnife");
        }
    }
}