using UnityEngine;
using UnityEngine.AI;
using static GameEvents;

/// <summary>
/// Pet that auto-follows the player using NavMesh.
/// Stops near the player when they stop.
/// Add a NavMeshAgent to this GameObject.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Pet : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float followRange = 5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float stopDistance = 1.5f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite followSprite;

    private Transform _player;
    private bool _following;
    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = stopDistance;
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }


    private void Update()
    {
        if (_player == null)
        {
            FindPlayer();
            return;
        }

        float dist = Vector3.Distance(transform.position, _player.position);

        if (!_following)
        {
            if (dist <= followRange)
            {
                EventBus.Publish(new DogGrabbed { });

                _following = true;
                _agent.isStopped = false;
                if (spriteRenderer != null && followSprite != null)
                    spriteRenderer.sprite = followSprite;
            }
            return;
        }

        if (dist <= stopDistance)
        {
            _agent.isStopped = true;
        }
        else
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);

            if (_agent.velocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(_agent.velocity.x, _agent.velocity.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(90f, angle-180f, 0f);
            }
        }
    }

    private void FindPlayer()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
            _player = player.transform;
    }
}