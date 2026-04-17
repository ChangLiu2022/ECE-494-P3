using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GameEvents;

public class SirenController : MonoBehaviour
{
    [SerializeField] private int flashCount = 3;
    [SerializeField] private float flashInterval = 0.2f;

    private Graphic graphic;
    private Coroutine flashRoutine;


    private void Start()
    {
        graphic = GetComponent<Graphic>();
        if (graphic == null) Debug.Log("Didn't find graphic for the siren!");
        else graphic.enabled = false;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AlertEvent>(OnAlertEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AlertEvent>(OnAlertEvent);
    }

    private void OnAlertEvent(AlertEvent e)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        if (graphic != null) graphic.enabled = true;
        flashRoutine = StartCoroutine(FlashLights());
    }

    private IEnumerator FlashLights()
    {
        if (graphic == null)
            yield break;

        Color red = new Color(1f, 0f, 0f, 27f / 255f);
        Color blue = new Color(0f, 0f, 1f, 27f / 255f);

        for (int i = 0; i < flashCount; i++)
        {
            graphic.color = red;
            yield return new WaitForSeconds(flashInterval);

            graphic.color = blue;
            yield return new WaitForSeconds(flashInterval);
        }

        graphic.enabled = false;
    }
}
