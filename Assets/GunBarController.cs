using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using static GameEvents;
using static GunEvents;
using UnityEngine.SceneManagement;

public class GunBarController : MonoBehaviour
{
    [Header("Segments")]
    [SerializeField] int initialSegments = 2;

    public float displayedProgress { get; private set; }
    private float progress = 0f; // TRUE value

    [Header("Decay")]
    [SerializeField] float decayDelay = 2f;
    [SerializeField] float decayRate = 0.2f;
    [SerializeField] float penalty = 0.75f;

    [Header("Upgrade/Downgrade Images")]
    [SerializeField] Image upgrade;
    [SerializeField] Image downgrade;

    [Header("Sprites")]
    [SerializeField] Sprite pistol_icon;
    [SerializeField] Sprite shotgun_icon;
    [SerializeField] Sprite rifle_icon;


    private float decayTimer = 0f;
    private bool isActive = false;

    private int maxSegments;
    private int upgradeLevel = 0;

    private bool isToasting = false;

    private bool hasBeenMaxLevel = false;

    void OnEnable()
    {
        EventBus.Subscribe<GuardShotEvent>(OnGuardKilled);
        EventBus.Subscribe<FirstHitEvent>(OnFirstHit);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<GuardShotEvent>(OnGuardKilled);
        EventBus.Unsubscribe<FirstHitEvent>(OnFirstHit);
    }

    private void Start()
    {
        maxSegments = initialSegments;
        if(SceneManager.GetActiveScene().name != "Safehouse" || SafehouseState.gun_collected) EventBus.Publish(new FirstHitEvent());
    }

    void Update()
    {
        // CHEATS
        if (Input.GetKeyDown(KeyCode.Equals)) EventBus.Publish(new GuardShotEvent());

        if (!isActive) return;

        HandleDecay();
        UpdateUI();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryUpgrade();
        }

        if(progress == 1f && !isToasting && upgradeLevel != 2)
        {
            isToasting = true;
            InformationBoxController.instance.Show("Press space to upgrade your weapon.");
            StartCoroutine(WaitFor5Seconds());
        }

        if (upgradeLevel == 2 && !isToasting && !hasBeenMaxLevel)
        {
            hasBeenMaxLevel = true;
            InformationBoxController.instance.Show("You've reached the max level!");
        }
    }

    IEnumerator WaitFor5Seconds()
    {
        yield return new WaitForSecondsRealtime(5f);
        isToasting = false;
        yield return null;
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

            // Step 4: convert back to normalized progress (0�1)
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

        // CLAMP and CHECK for downgrade
        if (progress <= 0f)
        {
            progress = 0f;

            // NEW: Handle downgrade if upgraded
            if (upgradeLevel > 0)
            {
                Downgrade();
            }
        }
        else
        {
            progress = Mathf.Clamp01(progress);
        }
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
        if (upgradeLevel == 2) return;

        Debug.Log("Upgrade Activated!");

        EventBus.Publish(new UpgradeActivatedEvent());

        progress = 0f;
        displayedProgress = 0f;

        maxSegments *= 2;
        upgradeLevel++;

        upgrade.sprite = rifle_icon;
        if (upgradeLevel == 1)
        {
            downgrade.sprite = pistol_icon;
            EventBus.Publish(new WeaponChangedEvent("Shotgun"));
        }
        else if (upgradeLevel == 2)
        {
            downgrade.sprite = shotgun_icon;
            EventBus.Publish(new WeaponChangedEvent("Rifle"));
        }
    }

    void Downgrade()
    {
        Debug.Log("Downgrade Activated!");

        EventBus.Publish(new DowngradeActivatedEvent());

        maxSegments /= 2; // revert max segments
        progress = 1f * penalty; // refill bar for the downgraded level
        displayedProgress = 1f * penalty;

        decayTimer = decayDelay;
        
        upgradeLevel--;

        downgrade.sprite = pistol_icon;
        if (upgradeLevel == 1)
        {
            upgrade.sprite = rifle_icon;
            EventBus.Publish(new WeaponChangedEvent("Shotgun"));
        }
        else if (upgradeLevel == 0)
        {
            upgrade.sprite = shotgun_icon;
            EventBus.Publish(new WeaponChangedEvent("Pistol"));
        }
    }
}