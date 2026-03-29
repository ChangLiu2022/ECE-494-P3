using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponUIManager : MonoBehaviour
{
    [Header("Weapon GameObjects")]
    [SerializeField] private GameObject pistol;
    [SerializeField] private GameObject shotgun;
    [SerializeField] private GameObject rifle;

    [Header("Weapon UI")]
    [SerializeField] private GameObject ammoDisplay;

    [Header("Player Sprites")]
    [SerializeField] private Sprite holsteredSprite;
    [SerializeField] private Sprite drawnSprite;

    [Header("Weapon UI Image")]
    [SerializeField] private Image weaponUIImage; // UI Image for active weapon
    [SerializeField] private Sprite pistolSprite;
    [SerializeField] private Sprite shotgunSprite;
    [SerializeField] private Sprite rifleSprite;

    // Player sprite renderer
    private SpriteRenderer playerSpriteRenderer;

    private string[] weapons = { "Pistol", "Shotgun", "Rifle" };
    private int currentIndex = -1; // -1 = none selected

    // Unlock state
    private bool pistolUnlocked = false;
    private bool shotgunUnlocked = false;
    private bool rifleUnlocked = false;
    private bool isHolstered = false;

    private void OnEnable()
    {
        EventBus.Subscribe<GunEvents.WeaponChangedEvent>(OnWeaponChanged);
        EventBus.Subscribe<GunEvents.PistolUnlockedEvent>(OnPistolUnlocked);
        EventBus.Subscribe<GunEvents.ShotgunUnlockedEvent>(OnShotgunUnlocked);
        EventBus.Subscribe<GunEvents.RifleUnlockedEvent>(OnRifleUnlocked);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GunEvents.WeaponChangedEvent>(OnWeaponChanged);
        EventBus.Unsubscribe<GunEvents.PistolUnlockedEvent>(OnPistolUnlocked);
        EventBus.Unsubscribe<GunEvents.ShotgunUnlockedEvent>(OnShotgunUnlocked);
        EventBus.Unsubscribe<GunEvents.RifleUnlockedEvent>(OnRifleUnlocked);
    }

    private void Start()
    {
        GameObject player = GameObject.Find("Body");
        if (player != null)
        {
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
        }

        UpdateWeaponUI();
        DisableAllWeapons();
        UpdatePlayerSprite();
        UpdateAmmoDisplay();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            TrySwapWeapon();

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            ToggleHolster();

        // Cheat codes for unlocking
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EventBus.Publish(new GunEvents.PistolUnlockedEvent());
        if (Input.GetKeyDown(KeyCode.Alpha2))
            EventBus.Publish(new GunEvents.ShotgunUnlockedEvent());
        if (Input.GetKeyDown(KeyCode.Alpha3))
            EventBus.Publish(new GunEvents.RifleUnlockedEvent());
    }

    private void ToggleHolster()
    {
        isHolstered = !isHolstered;

        if (isHolstered)
        {
            DisableAllWeapons();
        }
        else if (currentIndex >= 0)
        {
            ActivateWeaponByIndex(currentIndex);
        }

        UpdateWeaponUI();
        UpdatePlayerSprite();
        UpdateAmmoDisplay();
    }

    private void TrySwapWeapon()
    {
        List<int> available = new List<int>();
        if (pistolUnlocked) available.Add(0);
        if (shotgunUnlocked) available.Add(1);
        if (rifleUnlocked) available.Add(2);

        if (available.Count == 0)
        {
            EventBus.Publish(new GunEvents.WeaponChangedEvent("None"));
            return;
        }

        int nextIndex = currentIndex;
        do
        {
            nextIndex = (nextIndex + 1) % weapons.Length;
        } while (!available.Contains(nextIndex));

        currentIndex = nextIndex;

        if (!isHolstered)
            EventBus.Publish(new GunEvents.WeaponChangedEvent(weapons[currentIndex]));

        UpdateWeaponUI();
    }

    private void OnWeaponChanged(GunEvents.WeaponChangedEvent evt)
    {
        currentIndex = System.Array.IndexOf(weapons, evt.new_weapon);

        if (!isHolstered)
        {
            ActivateWeaponByIndex(currentIndex);
        }
        else
        {
            DisableAllWeapons();
        }

        UpdateWeaponUI();
        UpdatePlayerSprite();
        UpdateAmmoDisplay();
    }

    private void OnPistolUnlocked(GunEvents.PistolUnlockedEvent evt)
    {
        pistolUnlocked = true;
        SetFirstWeaponIfNone(0);
    }

    private void OnShotgunUnlocked(GunEvents.ShotgunUnlockedEvent evt)
    {
        shotgunUnlocked = true;
        SetFirstWeaponIfNone(1);
    }

    private void OnRifleUnlocked(GunEvents.RifleUnlockedEvent evt)
    {
        rifleUnlocked = true;
        SetFirstWeaponIfNone(2);
    }

    private void SetFirstWeaponIfNone(int index)
    {
        if (currentIndex == -1)
        {
            currentIndex = index;
            EventBus.Publish(new GunEvents.WeaponChangedEvent(weapons[index]));
        }
    }

    private void UpdateWeaponUI()
    {
        if (weaponUIImage == null)
            return;

        Sprite activeSprite = null;

        switch (currentIndex)
        {
            case 0: activeSprite = pistolSprite; break;
            case 1: activeSprite = shotgunSprite; break;
            case 2: activeSprite = rifleSprite; break;
        }

        if (activeSprite == null)
        {
            weaponUIImage.sprite = null;
            weaponUIImage.color = new Color(1, 1, 1, 0); // hide if no sprite
            return;
        }

        weaponUIImage.sprite = activeSprite;
        weaponUIImage.color = isHolstered ? new Color(1, 1, 1, 0) : Color.white;
    }

    private void ActivateWeapon(GameObject activeWeapon)
    {
        if (pistol != null) pistol.SetActive(activeWeapon == pistol);
        if (shotgun != null) shotgun.SetActive(activeWeapon == shotgun);
        if (rifle != null) rifle.SetActive(activeWeapon == rifle);
    }

    private void ActivateWeaponByIndex(int index)
    {
        if (index == 0) ActivateWeapon(pistol);
        else if (index == 1) ActivateWeapon(shotgun);
        else if (index == 2) ActivateWeapon(rifle);
        else DisableAllWeapons();
    }

    private void DisableAllWeapons()
    {
        if (pistol != null) pistol.SetActive(false);
        if (shotgun != null) shotgun.SetActive(false);
        if (rifle != null) rifle.SetActive(false);
    }

    private void UpdateAmmoDisplay()
    {
        if (ammoDisplay == null) return;

        ammoDisplay.SetActive(!isHolstered && currentIndex != -1);
    }

    private void UpdatePlayerSprite()
    {
        if (playerSpriteRenderer == null) return;

        playerSpriteRenderer.sprite = (isHolstered || currentIndex == -1) ? holsteredSprite : drawnSprite;
    }
}