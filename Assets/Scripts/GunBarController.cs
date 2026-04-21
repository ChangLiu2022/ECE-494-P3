using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameEvents;
using static GunEvents;

public class GunBarController : MonoBehaviour
{

    public float displayedProgress { get; private set; }
    private float progress = 0f; // TRUE value

    [Header("Decay")]
    [SerializeField] float decayDelay = 2f;
    [SerializeField] float decayDelayAfterUpgrade = 10f; 
    [SerializeField] float decayRate = 0.2f;
    [SerializeField] float penalty = 0.75f;

    [Header("Upgrade Information")]
    [SerializeField] Image upgrade;
    [SerializeField] TMP_Text next;
    [SerializeField] TMP_Text maxed;

    [Header("Upgrade Amounts")]
    [SerializeField] int forShotgun = 10;
    [SerializeField] int forRifle = 20;
    [SerializeField]int toMaxRifle = 50;

    [Header("Sprites")]
    [SerializeField] Sprite pistol_icon;
    [SerializeField] Sprite shotgun_icon;
    [SerializeField] Sprite rifle_icon;


    private float decayTimer = 0f;
    private bool isActive = false;

    private int maxSegments;
    private int upgradeLevel = 0;
    private float currentDecayDelay;

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
        maxSegments = forShotgun;
        if (SceneManager.GetActiveScene().name != "Safehouse" || SafehouseState.gun_collected) 
            EventBus.Publish(new FirstHitEvent());
    }


    void Update()
    {
        // CHEATS
        // if (Input.GetKeyDown(KeyCode.Equals)) EventBus.Publish(new GuardShotEvent());
        // if (Input.GetKeyDown(KeyCode.Equals)) SafehouseState.gun_bar_mult++;

        if (!isActive) return;

        HandleDecay();
        UpdateUI();
        TryUpgrade();
    }

    void OnFirstHit(FirstHitEvent e)
    {
        isActive = true;
    }

    void OnGuardKilled(GuardShotEvent e)
    {
        if (!isActive) return;

        if (displayedProgress == 0f) progress = SafehouseState.gun_bar_mult / maxSegments;
        else
        {
            // Step 1: convert to segment space
            float progressInSeg = displayedProgress * maxSegments;

            // Step 2: get current segment index
            int indexToStart = Mathf.FloorToInt(progressInSeg);
            indexToStart = indexToStart - (indexToStart % (int)SafehouseState.gun_bar_mult);

            // Step 3: add multiplier and clamp
            float finalIndex = Mathf.Min(indexToStart + SafehouseState.gun_bar_mult, maxSegments);

            // Step 4: convert back to normalized progress
            progress = finalIndex / maxSegments;
        }

        currentDecayDelay = decayDelay * GunUpgrades.GetCooldownMultiplier((GunUpgrades.Weapon)upgradeLevel);
        decayTimer = currentDecayDelay;
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


        EventBus.Publish(new UpgradeActivatedEvent());

        progress = 0;
        displayedProgress = 0;

        currentDecayDelay = decayDelayAfterUpgrade;
        decayTimer = currentDecayDelay;

        upgradeLevel++;

        
        if (upgradeLevel == 1)
        {
            maxSegments = forRifle;
            upgrade.sprite = rifle_icon;
            EventBus.Publish(new WeaponChangedEvent("Shotgun"));
        }
        else if (upgradeLevel == 2)
        {
            maxSegments = toMaxRifle;
            upgrade.enabled = false; // hide upgrade image at max level
            next.enabled = false;
            maxed.enabled = true;
            SafehouseState.reached_rifle = true;
            EventBus.Publish(new WeaponChangedEvent("Rifle"));
        }
    }

    void Downgrade()
    {

        EventBus.Publish(new DowngradeActivatedEvent());

        progress = 1f * penalty; // refill bar for the downgraded level
        displayedProgress = 1f * penalty;

        decayTimer = decayDelay * GunUpgrades.GetCooldownMultiplier((GunUpgrades.Weapon)upgradeLevel);

        upgradeLevel--;

        if (upgradeLevel == 1)
        {
            maxSegments = forRifle;
            upgrade.enabled = true;
            upgrade.sprite = rifle_icon;
            next.enabled = true;
            maxed.enabled = false;
            EventBus.Publish(new WeaponChangedEvent("Shotgun"));
        }
        else if (upgradeLevel == 0)
        {
            maxSegments = forShotgun;
            upgrade.sprite = shotgun_icon;
            EventBus.Publish(new WeaponChangedEvent("Pistol"));
        }
    }
}