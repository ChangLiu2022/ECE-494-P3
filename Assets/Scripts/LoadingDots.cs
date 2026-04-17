using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingDots : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float interval = 0.5f;

    private IEnumerator Start()
    {
        int dotCount = 0;

        while (true)
        {
            dotCount = (dotCount + 1) % 4;

            text.text = new string('.', dotCount);

            yield return new WaitForSecondsRealtime(interval);
        }
    }
}