using UnityEngine;

public class WhiteboardController : MonoBehaviour
{
    [SerializeField] Whiteboard whiteboard;
    private bool inRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = false;
        }
    }

    void Update()
    {
        if (inRange && Input.GetKeyDown(KeyCode.E))
        {
            if (HUDController.instance != null && HUDController.instance.IsEscapeOpen)
                HUDController.instance.ForceCloseEscape();
            if(whiteboard != null) whiteboard.ToggleBoard();
        }
    }
}
