using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapImageController : MonoBehaviour
{
    [SerializeField] Sprite map_one;
    [SerializeField] Sprite map_two;
    [SerializeField] Sprite map_three;

    private Image map_img;

    private void Awake()
    {
        map_img = GetComponent<Image>();
        if (map_img == null) Debug.Log("No Image component found on map!");
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (map_img == null) return;

        if (!SafehouseState.completed_tutorial && !SafehouseState.completed_newmap) map_img.sprite = map_one;
        else if (!SafehouseState.completed_newmap) map_img.sprite = map_two;
        else map_img.sprite = map_three;
    }
}
