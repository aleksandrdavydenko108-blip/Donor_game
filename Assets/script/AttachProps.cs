using System.Collections;
using UnityEngine;

public class AttachProps : MonoBehaviour
{
    public Transform rightHand; // mixamorig:RightHand Ч ручка
    public Transform leftHand;  // mixamorig:LeftHand Ч бумага

    public GameObject paper;
    public GameObject pen;

    public Vector3 paperOffset = new Vector3(0, 0, 0);
    public Vector3 paperRotation = new Vector3(0, 0, 0);
    public Vector3 penOffset = new Vector3(0, 0, 0);
    public Vector3 penRotation = new Vector3(0, 0, 0);

    public void AttachToHand()
    {
        StartCoroutine(AttachAfterDelay());
    }

    IEnumerator AttachAfterDelay()
    {
        yield return new WaitForSeconds(1.25f);

        // Ѕумага в левую руку
        paper.transform.SetParent(leftHand);
        paper.transform.localPosition = paperOffset;
        paper.transform.localEulerAngles = paperRotation;

        // –учка в правую руку
        pen.transform.SetParent(rightHand);
        pen.transform.localPosition = penOffset;
        pen.transform.localEulerAngles = penRotation;
    }
}