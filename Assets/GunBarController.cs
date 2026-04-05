using UnityEngine;
using static GameEvents;

public class GunBarController : MonoBehaviour
{
    [Header("Segments")]
    [SerializeField] public int initialSegments = 2;

    public float displayedProgress { get; private set; }
    private float progress = 0f; // TRUE value

    [Header("Decay")]
    [SerializeField] float decayDelay = 2f;
    [SerializeField] float decayRate = 0.2f;

    private float decayTimer = 0f;
    private bool isActive = false;

    private int maxSegments;

    void OnEnable()
    {
        EventBus.Subscribe<GuardKilledEvent>(OnGuardKilled);
        EventBus.Subscribe<FirstHitEvent>(OnFirstHit);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<GuardKilledEvent>(OnGuardKilled);
        EventBus.Unsubscribe<FirstHitEvent>(OnFirstHit);
    }

    private void Start()
    {
        maxSegments = initialSegments;
    }

    void Update()
    {
        // CHEATS
        if (Input.GetKeyDown(KeyCode.Alpha1)) EventBus.Publish(new FirstHitEvent());
        if (Input.GetKeyDown(KeyCode.Alpha2)) EventBus.Publish(new GuardShotEvent());

        if (!isActive) return;

        HandleDecay();
        UpdateUI();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryUpgrade();
        }
    }

    void OnFirstHit(FirstHitEvent e)
    {
        isActive = true;
    }

    void OnGuardKilled(GuardShotEvent e)
    {
        if (!isActive) return;

        if(displayedProgress == 0f) progress = 1f / maxSegments;
        else
        {
            // Step 1: map displayedProgress to segment space
            float progressInSegments = (displayedProgress * maxSegments) + (1f / maxSegments) * 0.01f;

            // Step 2: ceil to next segment
            int nextSegmentIndex = Mathf.CeilToInt(progressInSegments);

            // Step 3: clamp to maxSegments
            nextSegmentIndex = Mathf.Min(nextSegmentIndex, maxSegments);

            // Step 4: convert back to normalized progress (0–1)
            progress = (float)nextSegmentIndex / maxSegments;
            Debug.Log(progress.ToString());
            
        }

        decayTimer = decayDelay;
    }

    void HandleDecay()
    {
        if (decayTimer > 0)
        {
            decayTimer -= Time.deltaTime;
            return;
        }

        // Smooth decay of REAL value
        progress -= decayRate * Time.deltaTime;
        progress = Mathf.Clamp01(progress);
    }

    void UpdateUI()
    {
        displayedProgress = Mathf.MoveTowards(displayedProgress, progress, 3f * Time.deltaTime);
    }

    void TryUpgrade()
    {
        if (progress >= 1f)
        {
            ActivateUpgrade();
        }
    }

    void ActivateUpgrade()
    {
        Debug.Log("Upgrade Activated!");

        EventBus.Publish(new UpgradeActivatedEvent());

        progress = 0f;
        displayedProgress = 0f;

        maxSegments *= 2;
    }
}