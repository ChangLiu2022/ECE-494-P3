using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;


public class MenuController : MonoBehaviour
{
    [SerializeField] string sceneName;

    [Header("Fade Settings")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] float fadeDuration = 1f;

    private void Awake()
    {
        Cursor.visible = false;
    }

    public void Play()
    {
        if(FadeManager.Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        } else FadeManager.Instance.StartTransition(sceneName, musicSource, fadeDuration);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
