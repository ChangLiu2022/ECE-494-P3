using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;


public class MenuController : MonoBehaviour
{
    [SerializeField] string sceneName;

    [Header("Fade Settings")]
    [SerializeField] Image blackoutPane;
    [SerializeField] float fadeDuration = 1f;
    [SerializeField] AnimationCurve fadeCurve;
    [SerializeField] AudioSource musicSource;

    private bool isLoading = false;


    private void Awake()
    {
        Cursor.visible = false;

        if (blackoutPane != null)
        {
            Color c = blackoutPane.color;
            c.a = 0f;
            blackoutPane.color = c;
            
            blackoutPane.raycastTarget = false;
        }
    }

    public void Play()
    {
        if(blackoutPane == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        } else
        {
            if (!isLoading)
            {
                isLoading = true;
                blackoutPane.raycastTarget = true;
                StartCoroutine(FadeAndLoad());
            }
        }
    }

    private IEnumerator FadeAndLoad()
    {
        float timer = 0f;
        float initialVolume = musicSource != null ? musicSource.volume : 1f;

        Color c = blackoutPane.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration); 
            float curveValue = fadeCurve.Evaluate(t);

            float alpha = curveValue;

            c.a = alpha;

            blackoutPane.color = c;

            if (musicSource != null) musicSource.volume = Mathf.Lerp(initialVolume, 0f, curveValue);

            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
