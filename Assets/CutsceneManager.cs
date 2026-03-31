using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameEvents;

// Cutscene node represents a single camera position + rotation + timing
[System.Serializable]
public struct CutsceneNode
{
    public Vector3 position;       // target position
    public float duration;         // time to stay / move
    public float pauseTime;        // time to pause at target
}

// Event for triggering a cutscene
public struct CutsceneEvent
{
    public List<CutsceneNode> nodes;

    public CutsceneEvent(List<CutsceneNode> nodes)
    {
        this.nodes = nodes;
    }
}

// The CutsceneManager handles playing cutscenes
public class CutsceneManager : MonoBehaviour
{
    [Header("Camera Setup")]
    [SerializeField] private GameObject mainCamera; // camera or object to move
    [SerializeField] private float fixedY = 10f; // Y height for top-down camera

    [Header("Cutscene Locations")]
    [SerializeField] private Transform location0;
    [SerializeField] private Transform location1;
    [SerializeField] private Transform location2;
    [SerializeField] private Transform location3;

    [Header("Timing")]
    [SerializeField] private float nodeDuration = 2f;
    [SerializeField] private float nodePause = 1f;

    [Header("Dependencies")]
    [SerializeField] private MapController mapController;

    private Coroutine currentCutscene;
    private Transform cameraTransform;
    private CameraFollow followScript;

    public static bool did_we_already_watch_this_shit = false;

    private void OnEnable()
    {
        EventBus.Subscribe<CutsceneEvent>(OnCutsceneEvent);

        if (did_we_already_watch_this_shit == true)
            return;

        else
            did_we_already_watch_this_shit = true;

        if (mainCamera != null) cameraTransform = mainCamera.transform;
        if(mainCamera != null) followScript = mainCamera.GetComponent<CameraFollow>();
        StartCoroutine(PublishCutsceneNextFrame());
    }

    private IEnumerator PublishCutsceneNextFrame()
    {
        yield return null; // wait one frame
        PublishTestCutscene();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CutsceneEvent>(OnCutsceneEvent);
    }

    private void PublishTestCutscene()
    {
        var nodes = new List<CutsceneNode>();
        if (location0 != null) nodes.Add(CreateNode(location0));
        if (location1 != null) nodes.Add(CreateNode(location1));
        if (location2 != null) nodes.Add(CreateNode(location2));
        if (location3 != null) nodes.Add(CreateNode(location3));

        EventBus.Publish(new CutsceneEvent(nodes));
    }

    // Helper to create a node with fixed Y
    private CutsceneNode CreateNode(Transform t)
    {
        Vector3 pos = new Vector3(t.position.x, fixedY, t.position.z); // keep Y constant
        return new CutsceneNode
        {
            position = pos,
            duration = nodeDuration,
            pauseTime = nodePause
        };
    }

    private void OnCutsceneEvent(CutsceneEvent cutsceneEvent)
    {
        PlayCutscene(cutsceneEvent.nodes);
    }

    public void PlayCutscene(List<CutsceneNode> nodes)
    {
        if (nodes == null || nodes.Count == 0) return;

        if (mapController != null) mapController.enabled = false;
        EventBus.Publish(new GameFreezeEvent()); // Freeze the game during cutscene
        if (followScript != null) followScript.enabled = false;
        if (currentCutscene != null)
            StopCoroutine(currentCutscene);

        currentCutscene = StartCoroutine(RunCutscene(nodes));
    }

    private IEnumerator RunCutscene(List<CutsceneNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.pauseTime > 0f)
            {
                yield return new WaitForSecondsRealtime(node.pauseTime);
            }

            Vector3 startPos = cameraTransform.position;
            Quaternion startRot = cameraTransform.rotation;

            float elapsed = 0f;
            float duration = Mathf.Max(node.duration, 0.0001f);
            float speed = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime * speed;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 targetPos = new Vector3(node.position.x, fixedY, node.position.z); // top-down Y
                cameraTransform.position = Vector3.Lerp(startPos, targetPos, t);

                // Keep rotation constant for top-down, or interpolate if needed
                cameraTransform.rotation = startRot;

                yield return null;
            }

            cameraTransform.position = new Vector3(node.position.x, fixedY, node.position.z);
            cameraTransform.rotation = startRot;
        }

        if (mapController != null) mapController.enabled = true;
        EventBus.Publish(new GameUnfreezeEvent()); // Unfreeze after cutscene
        if (followScript != null) followScript.enabled = true;
        currentCutscene = null;
    }
}