using System.Collections;
using UnityEngine;

public class KnifePickupInteraction : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Animator ggAnimator;
    [SerializeField] private MonoBehaviour firstPersonMovementScript;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private MeatHighlight meatHighlight;

    [Header("Настройки")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float moveToPointDuration = 0.3f;

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
            StartCoroutine(TakeKnifeRoutine());
        }
    }

    private IEnumerator TakeKnifeRoutine()
    {
        knifeTaken = true;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        if (meatHighlight != null)
            meatHighlight.DisableHighlight();

        if (firstPersonMovementScript != null)
            firstPersonMovementScript.enabled = false;

        CharacterController cc = null;
        if (interactionPoint != null && playerTransform != null)
        {
            cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 startPos = playerTransform.position;
            Quaternion startRot = playerTransform.rotation;

            Vector3 targetPos = interactionPoint.position;
            Quaternion targetRot = interactionPoint.rotation;

            float elapsed = 0f;
            while (elapsed < moveToPointDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveToPointDuration);

                playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
                playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            playerTransform.position = targetPos;
            playerTransform.rotation = targetRot;

            if (cc != null) cc.enabled = true;
        }

        if (ggAnimator != null)
        {
            ggAnimator.SetBool("HasKnife", true);
            ggAnimator.SetTrigger("TakeKnife");
        }
    }
}