using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField] private string cutsceneSceneName;
    [SerializeField] private float fadeDuration = 1f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider coll)
    {
        if (triggered) return;
        if (!coll.CompareTag("Player")) return;

        triggered = true;
        FadeManager.Instance.StartTransition(cutsceneSceneName, null, fadeDuration);
    }
}