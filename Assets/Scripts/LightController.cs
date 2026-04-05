using UnityEngine;
using static GameEvents;

public class LightController : MonoBehaviour
{
    [Header("Spotlight Parent")]
    public GameObject spotlightParent;

    void Start()
    {
        if (spotlightParent != null) spotlightParent.SetActive(true);
    }

    void OnEnable()
    {
        EventBus.Subscribe<PowerOffEvent>(OnPowerOff);
        EventBus.Subscribe<PowerOnEvent>(OnPowerON);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PowerOffEvent>(OnPowerOff);
        EventBus.Unsubscribe<PowerOnEvent>(OnPowerON);
    }

    void OnPowerOff(PowerOffEvent e)
    {
        if (spotlightParent != null) spotlightParent.SetActive(false);
    }

    void OnPowerON(PowerOnEvent e)
    {
        if (spotlightParent != null) spotlightParent.SetActive(true);
    }
}
