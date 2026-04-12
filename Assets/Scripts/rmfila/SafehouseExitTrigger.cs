using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SafehouseExitTrigger : MonoBehaviour
{
    [SerializeField] private string tutorial_scene = "Tutorial";
    [SerializeField] private string newmap_scene = "NewMap";
    [SerializeField] private string not_ready_message = "Collect the gun and map before leaving.";
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Canvas canvas;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Body"))
            return;

        if (!SafehouseState.gun_collected || !SafehouseState.paper_collected)
        {
            InformationBoxController.instance.Show(not_ready_message);
            return;
        }

        string nextScene = !SafehouseState.completed_tutorial ? tutorial_scene : newmap_scene;
        StartCoroutine(FadeAndLoadScene(nextScene));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        var fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvas.transform, false);

        var fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        var rect = fadeObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        fadeImage.color = Color.black;
        SceneManager.LoadScene(sceneName);
    }
}