using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookController : MonoBehaviour
{
    public float sensitivityX = 2f;
    public float sensitivityY = 1f;
    public float horizontalLimit = 70f;

    private float rotX = -50.453f;
    private float rotY = 180f;

    private float totalMoved = 0f;
    public float requiredMovement = 80f;
    public bool HasLookedAround { get; private set; } = false;

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivityY;

        rotY += mouseX;
        rotX -= mouseY;

        rotX = Mathf.Clamp(rotX, -10f, 41.16f);
        rotY = Mathf.Clamp(rotY, 180f - horizontalLimit, 180f + horizontalLimit);

        transform.rotation = Quaternion.Euler(rotX, rotY, 0f);

        if (!HasLookedAround && rotX >= 25f)
        {
            HasLookedAround = true;
            Debug.Log("Игрок увидел шрам");
            PlayerThoughts thoughts = FindObjectOfType<PlayerThoughts>();
            thoughts.OnThoughtsFinished += () => FindObjectOfType<NurseController>().StartSequence();
            thoughts.StartThoughts();
        }
    }
}