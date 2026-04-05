using UnityEngine;

public class Workbench : MonoBehaviour
{
    [SerializeField] private Color flash_color = Color.yellow;
    [SerializeField] private float flash_speed = 3f;

    private MeshRenderer mesh_renderer;
    private Color[] original_base_colors;
    private Color[] original_emission_colors;

    private void Awake()
    {
        mesh_renderer = GetComponent<MeshRenderer>();

        original_base_colors = new Color[mesh_renderer.materials.Length];
        original_emission_colors = new Color[mesh_renderer.materials.Length];

        for (int i = 0; i < mesh_renderer.materials.Length; i++)
        {
            original_base_colors[i] = mesh_renderer.materials[i].GetColor("_BaseColor");
            original_emission_colors[i] = mesh_renderer.materials[i].GetColor("_EmissionColor");
        }
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * flash_speed) + 1f) / 2f;
        Color emission = Color.Lerp(Color.black, flash_color, t) * 0.6f;

        for (int i = 0; i < mesh_renderer.materials.Length; i++)
        {
            mesh_renderer.materials[i].SetColor("_BaseColor",
                Color.Lerp(original_base_colors[i], flash_color, t));

            mesh_renderer.materials[i].EnableKeyword("_EMISSION");
            mesh_renderer.materials[i].SetColor("_EmissionColor", emission);
        }
    }

    private void OnDisable()
    {
        if (mesh_renderer == null) return;

        for (int i = 0; i < mesh_renderer.materials.Length; i++)
        {
            mesh_renderer.materials[i].SetColor("_BaseColor", original_base_colors[i]);
            mesh_renderer.materials[i].SetColor("_EmissionColor", original_emission_colors[i]);
            mesh_renderer.materials[i].DisableKeyword("_EMISSION");
        }
    }
}
