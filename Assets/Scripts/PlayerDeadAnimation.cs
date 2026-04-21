using UnityEngine;
using UnityEngine.SceneManagement;
using static GameEvents;

public class PlayerDeadAnimation : MonoBehaviour
{
    [SerializeField] Sprite dead_sprite;
    private float scale = 0.3f;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) Debug.Log("No SpriteRenderer component attached to player body!");
        else sr.sortingOrder = 0;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
    }

    private void OnGameOver(GameOverEvent e)
    {
        EventBus.Publish(new GameFreezeEvent());

        if (sr != null)
        {
            sr.sprite = dead_sprite;
            sr.sortingOrder = -2;
            transform.localScale = new Vector3(scale, scale, 1f); 
            transform.rotation *= Quaternion.Euler(0f, 0f, 180f);
        }
        FadeManager.Instance.StartTransition(SceneManager.GetActiveScene().name, null, 1f);
    }

    //private IEnumerator WaitForSeconds(float seconds)
    //{
    //    yield return new WaitForSecondsRealtime(seconds);
    //    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    //}
}
