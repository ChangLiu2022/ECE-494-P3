using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpriteSwitcher : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] sprites;

    [Header("Timing")]
    [SerializeField] private float switchInterval = 0.2f;
    [SerializeField] private bool loop = true;

    private Image img;
    private int index = 0;

    void Awake()
    {
        img = GetComponent<Image>();

        if (img == null)
        {
            Debug.LogError("SpriteSwitcher requires an Image component!");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError("No sprites assigned to SpriteSwitcher!");
            return;
        }

        StartCoroutine(SwitchSprites());
    }

    private IEnumerator SwitchSprites()
    {
        while (true)
        {
            img.sprite = sprites[index];

            index++;

            if (index >= sprites.Length)
            {
                if (loop)
                    index = 0;
                else
                    yield break;
            }

            yield return new WaitForSecondsRealtime(switchInterval);
        }
    }
}