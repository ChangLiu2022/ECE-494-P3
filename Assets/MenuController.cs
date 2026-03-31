using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] string scene_name;
    private void Awake()
    {
        Cursor.visible = false;
    }

    public void Play()
    {
        SceneManager.LoadScene(scene_name);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
