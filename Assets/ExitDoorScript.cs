using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ExitDoorScript : MonoBehaviour
{
    [SerializeField] public float interactRange = 1.5f;
    [SerializeField] private string target_scene = "Safehouse";
    [SerializeField] private bool set_tutorial_complete = false;
    [SerializeField] private bool set_newmap_complete = false;
    [SerializeField] private string no_gold_message = "You need to collect the gold before leaving!";
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Canvas canvas;
    private bool exiting = false;

    private void OnTriggerEnter(Collider coll)
    {
        if (exiting) return;

        if (!coll.CompareTag("Player")) return;

        if (!PlayerWallet.level_reward_claimed)
        {
            InformationBoxController.instance.Show(no_gold_message);
            return;
        }

        exiting = true;

        if (set_tutorial_complete)
            SafehouseState.completed_tutorial = true;

        if (set_newmap_complete)
            SafehouseState.completed_newmap = true;

        PlayerWallet.AdvanceLevel();
        InformationBoxController.instance.gameObject.SetActive(false);
        StartCoroutine(FadeAndLoadScene(target_scene));
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