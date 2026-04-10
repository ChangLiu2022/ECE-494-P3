using UnityEngine;

public class BamController : MonoBehaviour
{
    [SerializeField] private GameObject bamObject;

    private void Awake()
    {
        if (bamObject != null)
            bamObject.SetActive(false);
    }

    public void Show()
    {
        if (bamObject != null)
            bamObject.SetActive(true);
    }

    public void Hide()
    {
        if (bamObject != null)
            bamObject.SetActive(false);
    }
}