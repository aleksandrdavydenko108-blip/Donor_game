using UnityEngine;

public class MeatHighlight : MonoBehaviour
{
    [SerializeField] private GameObject highlightObject;

    public void EnableHighlight()
    {
        if (highlightObject != null)
            highlightObject.SetActive(true);
    }

    public void DisableHighlight()
    {
        if (highlightObject != null)
            highlightObject.SetActive(false);
    }
}
