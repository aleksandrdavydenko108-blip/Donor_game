using System.Collections;
using UnityEngine;

public class NurseController : MonoBehaviour
{
    public Animator animator;
    public Transform wayPoint;
    public Transform bedTarget;
    public float walkSpeed = 1.5f;
    public NurseDialogue nurseDialogue;
    public AttachProps attachProps;

    public void StartSequence()
    {
        StartCoroutine(NurseRoutine());
    }

    IEnumerator NurseRoutine()
    {
        animator.SetTrigger("StartWalk");

        while (Vector3.Distance(transform.position, wayPoint.position) > 0.3f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                wayPoint.position,
                walkSpeed * Time.deltaTime
            );
            Vector3 direction1 = wayPoint.position - transform.position;
            direction1.y = 0;
            if (direction1 != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction1) * Quaternion.Euler(0, 30, 0);
            yield return null;
        }

        while (Vector3.Distance(transform.position, bedTarget.position) > 0.3f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                bedTarget.position,
                walkSpeed * Time.deltaTime
            );
            Vector3 direction2 = bedTarget.position - transform.position;
            direction2.y = 0;
            if (direction2 != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction2) * Quaternion.Euler(0, 25, 0);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        animator.SetTrigger("StartTalk");

        bool dialogueDone = false;
        nurseDialogue.OnDialogueFinished += () => dialogueDone = true;
        nurseDialogue.StartDialogue();
        yield return new WaitUntil(() => dialogueDone);

        animator.SetTrigger("GivePaper");

        // Запускаем прикрепление предметов
        attachProps.AttachToHand();
        FindObjectOfType<SignController>().ShowSignButton();
    }
}