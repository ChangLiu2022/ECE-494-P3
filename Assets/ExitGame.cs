using UnityEngine;
using static GameEvents;

public class ExitGame : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Body"))
            EventBus.Publish(new WinEvent());
    }
}
