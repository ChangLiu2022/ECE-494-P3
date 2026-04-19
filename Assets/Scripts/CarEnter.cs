using System.Collections;
using UnityEngine;
using static GameEvents;

public class CarEnter : MonoBehaviour
{
    [Header("Driving Settings")]
    [SerializeField] private float distance = 5f;   // how far to move
    [SerializeField] private float speed = 2f;      // units per second
    [SerializeField] private bool useLocalRight = true; // local vs world right
    [SerializeField] private Transform car_transform;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip door_closing;
    [SerializeField] private AudioClip car_driving;

    private Coroutine moveRoutine;

    private SpriteRenderer sr;
    private AudioSource audio;

    private static bool has_seen_entrance = false;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) Debug.Log("No SpriteRenderer component assigned to the Player Body!");

        audio = GetComponent<AudioSource>();
        if (audio == null) Debug.Log("No AudioSource component assigned to the Player Body!");

        if(!has_seen_entrance)
        {
            if (sr != null) sr.enabled = false;
            EventBus.Publish(new GameFreezeEvent());
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveRight());
        }
    }

    private IEnumerator MoveRight()
    {
        has_seen_entrance = true;
        if (audio != null) audio.PlayOneShot(car_driving);

        Vector3 targetPos = car_transform.position;
        Vector3 direction = useLocalRight ? car_transform.right : Vector3.right;
        Vector3 startPos = targetPos - direction.normalized * distance;

        car_transform.position = startPos;

        while (Vector3.Distance(car_transform.position, targetPos) > 0.01f)
        {
            car_transform.position = Vector3.MoveTowards(
                car_transform.position,
                targetPos,
                speed * Time.unscaledDeltaTime
            );

            yield return null;
        }

        // Snap exactly to target at the end
        car_transform.position = targetPos;
        moveRoutine = null;

        audio.Stop();
        audio.PlayOneShot(door_closing);
        yield return new WaitForSecondsRealtime(door_closing.length);

        if (sr != null) sr.enabled = true;
        EventBus.Publish(new GameUnfreezeEvent());
    }
}
