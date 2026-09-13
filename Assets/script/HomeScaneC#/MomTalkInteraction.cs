using UnityEngine;

public class MomTalkInteraction : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private MomWalkToLaundry momWalker;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private string question = "Ты уже порезал мясо?";
    [SerializeField] private float displayDuration = 3f;

    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            bool momReady = momWalker == null || momWalker.HasArrived;

            if (interactPrompt != null)
                interactPrompt.SetActive(momReady);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        bool momReady = momWalker == null || momWalker.HasArrived;
        if (!momReady) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (interactPrompt != null) interactPrompt.SetActive(false);

            dialogueManager.ShowSingleLine(DialogueManager.Speaker.Mom, question, displayDuration);
        }
    }
}

