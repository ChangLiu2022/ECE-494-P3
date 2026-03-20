using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameEvents;

public class LightController : MonoBehaviour
{
    public Light directionalLight;

    
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
        if (directionalLight != null)
            directionalLight.color = Color.black;
    }

    void OnPowerON(PowerOnEvent e)
    {
        if (directionalLight != null)
        {
            directionalLight.color = Color.white;
        }
    }
}