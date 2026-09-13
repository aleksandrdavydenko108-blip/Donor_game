using System.Collections;
using UnityEngine;

public class DialogueSceneController : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueCameraLook dialogueCameraLook;
    [SerializeField] private FadeInOnStart fadeController;
    [SerializeField] private Animator ggAnimator;
    [SerializeField] private MomWalkToLaundry momWalker;

    [Header("Что включить после диалога")]
    [SerializeField] private MonoBehaviour firstPersonMovementScript;
    [SerializeField] private float standUpDuration = 1.2f;

    [Header("Приземление после вставания")]
    [SerializeField] private Transform playerTransform;

    private void Start()
    {
        if (firstPersonMovementScript != null)
            firstPersonMovementScript.enabled = false;

        dialogueManager.OnDialogueStarted += HandleDialogueStarted;
        dialogueManager.OnDialogueEnded += HandleDialogueEnded;

        if (fadeController != null)
        {
            fadeController.OnFadeComplete += HandleFadeComplete;
        }
        else
        {
            dialogueManager.StartDialogue();
        }
    }

    private void HandleFadeComplete()
    {
        dialogueManager.StartDialogue();
    }

    private void HandleDialogueStarted()
    {
        dialogueCameraLook.EnableLook();

        if (firstPersonMovementScript != null)
            firstPersonMovementScript.enabled = false;
    }

    private void HandleDialogueEnded()
    {
        StartCoroutine(EnableMovementAfterStandUp());
    }

    private IEnumerator EnableMovementAfterStandUp()
    {
        yield return new WaitForSeconds(standUpDuration);

        if (ggAnimator != null)
        {
            ggAnimator.applyRootMotion = false;
        }

        SnapPlayerToFloor();

        dialogueCameraLook.DisableLook();

        if (firstPersonMovementScript != null)
            firstPersonMovementScript.enabled = true;

        if (momWalker != null)
            momWalker.StartWalking();
    }

    private void SnapPlayerToFloor()
    {
        if (playerTransform == null) return;

        StartCoroutine(SnapPlayerToFloorRoutine());
    }

    private IEnumerator SnapPlayerToFloorRoutine()
    {
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        bool hadController = false;

        if (cc != null)
        {
            hadController = cc.enabled;
            cc.enabled = false;
        }

        Vector3 pos = playerTransform.position;
        pos.y = 0f;
        playerTransform.position = pos;

        if (cc != null)
        {
            cc.enabled = hadController;
        }

        yield return null;

        pos = playerTransform.position;
        pos.y = 0f;
        playerTransform.position = pos;
    }
}