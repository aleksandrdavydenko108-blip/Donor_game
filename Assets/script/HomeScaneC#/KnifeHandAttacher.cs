using UnityEngine;

public class KnifeHandAttacher : MonoBehaviour
{
    [SerializeField] private Transform knife;
    [SerializeField] private Transform handBone;

    private bool attached = false;

    public void OnKnifeGrabMoment()
    {
        if (attached || knife == null || handBone == null) return;

        // true = сохраняем текущее мировое положение/поворот/масштаб ножа,
        // просто меняем родителя на кость руки - без скачков и без искажения размера
        knife.SetParent(handBone, true);

        attached = true;
    }
}