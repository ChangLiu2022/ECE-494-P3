using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Weapon UI Image")]
    [SerializeField] private Sprite pistolSprite;
    [SerializeField] private Sprite shotgunSprite;
    [SerializeField] private Sprite rifleSprite;

    private SpriteRenderer playerSpriteRenderer;

    private string[] weapons = { "Pistol", "Shotgun", "Rifle" };
    private int currentIndex = 0;
    
    private bool isHolstered = false;

    private void Start()
    {
        // Subscribe once at start - subscriptions persist even if this component is disabled/enabled
        EventBus.Subscribe<GunEvents.WeaponChangedEvent>(OnWeaponChanged);

        GameObject player = GameObject.Find("Body");
        if (player != null)
            playerSpriteRenderer = player.GetComponent<SpriteRenderer>();

        if (SceneManager.GetActiveScene().name == "Safehouse")
        {
            if (!SafehouseState.gun_collected)
            {
                currentIndex = 0;
                isHolstered = true;
                DisableAllWeapons();
            }
        }
        else
        {
            currentIndex = 0;
            isHolstered = false;
            ActivateWeaponByIndex(0);
        }

        UpdatePlayerSprite();
    }

    private void OnDestroy()
    {
        // Clean up subscriptions when component is destroyed
        EventBus.Unsubscribe<GunEvents.WeaponChangedEvent>(OnWeaponChanged);
    }

    private void OnWeaponChanged(GunEvents.WeaponChangedEvent evt)
    {
        currentIndex = System.Array.IndexOf(weapons, evt.new_weapon);

        isHolstered = false;
        ActivateWeaponByIndex(currentIndex);
        UpdatePlayerSprite();
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

    private void UpdatePlayerSprite()
    {
        if (playerSpriteRenderer == null) return;
        if (isHolstered) playerSpriteRenderer.sprite = holsteredSprite;
        else if (currentIndex == 0) playerSpriteRenderer.sprite = pistolSprite;
        else if (currentIndex == 1) playerSpriteRenderer.sprite = shotgunSprite;
        else if (currentIndex == 2) playerSpriteRenderer.sprite = rifleSprite;
    }
}
