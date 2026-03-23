using System.Collections;
using UnityEngine;

public class VehiclePit : MonoBehaviour
{
    [Header("Pit Settings")]
    [SerializeField] private float pitForce = 5f;
    [SerializeField] private int coinLoss = 10;
    [SerializeField] private float spinDuration = 1f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private LayerMask vehicleLayer;
    [SerializeField] private float iframeDuration = 2f;
    private VehicleMovement _movement;
    private bool _isPitted;
    private bool _wasActive;
    private bool _iframed;
    public void ClearActiveFlag() => _wasActive = false;
    public bool IsPitted => _isPitted || _iframed;
    private void Awake()
    {
        _movement = GetComponent<VehicleMovement>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;
        if (((1 << other.gameObject.layer) & vehicleLayer) == 0) return;

        Pit(pitForce, coinLoss);
    }

    public void Pit(float force, int coinLoss)
    {
        if (IsPitted) return;

        EventBus.Publish(new GameEvents.VehiclePitEvent());

        _wasActive = _movement.IsActive;
        _movement.SetActive(false);

        StartCoroutine(PitRoutine(force));
    }

    private IEnumerator PitRoutine(float force)
    {
        _isPitted = true;

        Vector3 pushDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            transform.Rotate(Vector3.up * spinSpeed * dt);

            float fade = 1f - (elapsed / spinDuration);
            transform.position += pushDir * force * fade * dt;

            yield return null;
        }

        _isPitted = false;

        if (_wasActive)
            _movement.SetActive(true);
        _iframed = true;
        yield return new WaitForSeconds(iframeDuration);
        _iframed = false;
    }
}