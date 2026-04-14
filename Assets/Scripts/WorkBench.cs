using UnityEngine;

public class Workbench : MonoBehaviour
{
    [SerializeField] private Color flash_color = Color.yellow;
    [SerializeField] private float flash_speed = 3f;

    private MeshRenderer mesh_renderer;
    private Material[] cached_mats;
    private Color[] original_base_colors;
    private Color[] original_emission_colors;

    private void Awake()
    {
        mesh_renderer = GetComponent<MeshRenderer>();
        cached_mats = mesh_renderer.materials; // cache ONCE — avoids per-frame allocation

        original_base_colors = new Color[cached_mats.Length];
        original_emission_colors = new Color[cached_mats.Length];

        for (int i = 0; i < cached_mats.Length; i++)
        {
            original_base_colors[i] = cached_mats[i].GetColor("_BaseColor");
            original_emission_colors[i] = cached_mats[i].GetColor("_EmissionColor");
        }
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * flash_speed) + 1f) / 2f;
        Color emission = Color.Lerp(Color.black, flash_color, t) * 0.6f;

        for (int i = 0; i < cached_mats.Length; i++)
        {
            cached_mats[i].SetColor("_BaseColor",
                Color.Lerp(original_base_colors[i], flash_color, t));
            cached_mats[i].EnableKeyword("_EMISSION");
            cached_mats[i].SetColor("_EmissionColor", emission);
        }
    }

    private void OnDisable()
    {
        if (cached_mats == null) return;

        for (int i = 0; i < cached_mats.Length; i++)
        {
            cached_mats[i].SetColor("_BaseColor", original_base_colors[i]);
            cached_mats[i].SetColor("_EmissionColor", original_emission_colors[i]);
            cached_mats[i].DisableKeyword("_EMISSION");
        }
    }
}
