using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private Image weaponUIImage;
    [SerializeField] private Sprite pistolSprite;
    [SerializeField] private Sprite shotgunSprite;
    [SerializeField] private Sprite rifleSprite;

    private SpriteRenderer playerSpriteRenderer;

    private string[] weapons = { "Pistol", "Shotgun", "Rifle" };
    private int currentIndex = -1;

    private bool pistolUnlocked = true;
    private bool shotgunUnlocked = false;
    private bool rifleUnlocked = false;
    private bool isHolstered = false;

    private void Start()
    {
        // Subscribe once at start - subscriptions persist even if this component is disabled/enabled
        EventBus.Subscribe<GunEvents.WeaponChangedEvent>(OnWeaponChanged);
        EventBus.Subscribe<GunEvents.PistolUnlockedEvent>(OnPistolUnlocked);
        EventBus.Subscribe<GunEvents.ShotgunUnlockedEvent>(OnShotgunUnlocked);
        EventBus.Subscribe<GunEvents.RifleUnlockedEvent>(OnRifleUnlocked);

        GameObject player = GameObject.Find("Body");
        if (player != null)
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

        if (SceneManager.GetActiveScene().name == "Safehouse")
        {
            if (SafehouseState.gun_collected)
            {
                currentIndex = 0;
                isHolstered = true;
            }
            DisableAllWeapons();
        }
        else
        {
            currentIndex = 0;
            isHolstered = false;
            ActivateWeaponByIndex(0);
        }

        UpdateWeaponUI();
        UpdatePlayerSprite();
        UpdateAmmoDisplay();
    }

    private void OnDestroy()
    {
        // Clean up subscriptions when component is destroyed
        EventBus.Unsubscribe<GunEvents.WeaponChangedEvent>(OnWeaponChanged);
        EventBus.Unsubscribe<GunEvents.PistolUnlockedEvent>(OnPistolUnlocked);
        EventBus.Unsubscribe<GunEvents.ShotgunUnlockedEvent>(OnShotgunUnlocked);
        EventBus.Unsubscribe<GunEvents.RifleUnlockedEvent>(OnRifleUnlocked);
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
        if (weaponUIImage == null) return;

        Sprite activeSprite = currentIndex switch
        {
            0 => pistolSprite,
            1 => shotgunSprite,
            2 => rifleSprite,
            _ => null
        };

        if (activeSprite == null)
        {
            weaponUIImage.sprite = null;
            weaponUIImage.color = new Color(1, 1, 1, 0);
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
