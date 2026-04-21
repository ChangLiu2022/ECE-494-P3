using UnityEngine;
using static GameEvents;

public class GuardDied : MonoBehaviour
{
    private void Awake()
    {
        EventBus.Publish(new GuardDead());
    }
}
