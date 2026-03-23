using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameEvents;

public class LaserController : MonoBehaviour
{

    private void OnEnable()
    {
        EventBus.Subscribe<LightsOutEvent>(OnLightsOutEvent);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<LightsOutEvent>(OnLightsOutEvent);
    }

    // disables laser if the lights are turned out!
    private void OnLightsOutEvent(LightsOutEvent e)
    {
        StartCoroutine(LaserBlinksOff());
    }

    // alerts guards if the laser is tripped!
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            EventBus.Publish(new AlertEvent());
            Debug.Log("Laser tripped!");
        }
    }

    private Renderer rend;
    private Material mat;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
        mat.EnableKeyword("_EMISSION");
    }

    [Header("Flicker Settings")]
    [SerializeField] private float initialIntensity = 5f;       // Starting glow
    [SerializeField] private float fadeDuration = 0.2f;             // How fast it dims
    [SerializeField] private float flickerChance = 0.3f;       // Probability of a flicker each frame
    [SerializeField] private Vector2 flickerDuration = new Vector2(0.02f, 0.06f); // min/max flicker time

    private IEnumerator LaserBlinksOff()
    {
        float intensity = initialIntensity;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / fadeDuration); // normalized 1->0
            intensity = initialIntensity * t;
            mat.SetColor("_EmissionColor", Color.red * intensity);

            if (Random.value < flickerChance)
            {
                rend.enabled = false;
                yield return new WaitForSeconds(Random.Range(flickerDuration.x, flickerDuration.y));
                rend.enabled = true;
            }

            yield return null;
        }

        // Final quick pops (short and snappy)
        for (int i = 0; i < 2; i++)
        {
            rend.enabled = false;
            yield return new WaitForSeconds(0.03f);
            rend.enabled = true;
            yield return new WaitForSeconds(0.03f);
        }

        // Fully off
        rend.enabled = false;
        gameObject.SetActive(false);
    }

    // for testing, REMOVE later
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            EventBus.Publish(new LightsOutEvent());
        }
    }
}
