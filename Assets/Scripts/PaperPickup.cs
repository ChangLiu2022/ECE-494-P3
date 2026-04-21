using System.Collections;
using UnityEngine;
using TMPro;

public class PaperPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickup_range = 2.5f;
    [SerializeField] private float flash_range = 5f;
    [SerializeField] private KeyCode pickup_key = KeyCode.E;
    [SerializeField] private Transform player;

    [Header("Flash Settings")]
    [SerializeField] private Color flash_color = Color.yellow;
    [SerializeField] private float flash_speed = 3f;

    private bool in_range = false;
    private bool in_flash_range = false;
    private MeshRenderer mesh_renderer;
    private Color[] original_colors;

    private void Awake()
    {
        mesh_renderer = GetComponent<MeshRenderer>();
        original_colors = new Color[mesh_renderer.materials.Length];
        for (int i = 0; i < mesh_renderer.materials.Length; i++)
            original_colors[i] = mesh_renderer.materials[i].color;
    }

    private void Start()
    {
        if (!SafehouseState.paper_collected) gameObject.GetComponent<MeshRenderer>().enabled = true;
        if (SafehouseState.completed_map_2)
        {
            SafehouseState.paper_collected = true;
            SafehouseState.paper_collected_once = true;
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (player == null || SafehouseState.paper_collected)
            return;

        float dist = Vector3.Distance(transform.position, player.position);
        in_range = dist <= pickup_range;
        in_flash_range = dist <= flash_range;

        if (in_range && Input.GetKeyDown(pickup_key))
        {
            SafehouseState.paper_collected = true;
            if (!SafehouseState.paper_collected_once) InformationBoxController.instance.Show("Press 'Tab' to view your map.");
            else InformationBoxController.instance.Show("You picked up the new map.");
            SafehouseState.paper_collected_once = true;

            gameObject.GetComponent<MeshRenderer>().enabled = false;
            return;
        }

        if (in_flash_range) PulseFlash();
        else ResetColors();
    }


    private void PulseFlash()
    {
        float t = (Mathf.Sin(Time.time * flash_speed) + 1f) / 2f;

        for (int i = 0; i < mesh_renderer.materials.Length; i++)
            mesh_renderer.materials[i].color = Color.Lerp(original_colors[i], flash_color, t);
    }

    private void ResetColors()
    {
        for (int i = 0; i < mesh_renderer.materials.Length; i++)
            mesh_renderer.materials[i].color = original_colors[i];
    }
}