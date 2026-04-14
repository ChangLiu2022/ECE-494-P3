using UnityEngine;
using static GameEvents;
using static GunEvents;

public class InvPistol : MonoBehaviour
{
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private float noise_range = 10f;

    [SerializeField] private LayerMask wallLayer;
    
    private float _nextFireTime;
    private HasInventory inv;

    private float base_fire_rate;
    private float effective_fire_rate;
    private int effective_damage;

    // sets the HasInventory component to use for ammo verification
    private void Awake()
    {
        base_fire_rate = fireRate;

        GameObject player = GameObject.Find("Player");

        if (player != null)
            inv = player.GetComponent<HasInventory>();

        RefreshStats();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<UpgradePurchasedEvent>(OnUpgrade);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<UpgradePurchasedEvent>(OnUpgrade);
    }

    private void OnUpgrade(UpgradePurchasedEvent e)
    {
        if (e.weapon == GunUpgrades.Weapon.Pistol) RefreshStats();
    }

    private void RefreshStats()
    {
        effective_fire_rate = base_fire_rate / GunUpgrades.GetFireRateMultiplier(GunUpgrades.Weapon.Pistol);
        effective_damage = Mathf.Max(1, Mathf.RoundToInt(GunUpgrades.GetDamageMultiplier(GunUpgrades.Weapon.Pistol)));
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextFireTime)
        {
            if (Physics.CheckSphere(firePoint.position, 0.1f, wallLayer))
            {
                return;
            }
            
            if (inv != null && inv.BulletCount <= 0)
            {
                EventBus.Publish(new FailedToFireEvent()); // to trigger HUD flashing
                return;
            }

            if (inv != null) inv.AddBullets(-1); // update bullet count

            _nextFireTime = Time.time + effective_fire_rate;

            Vector3 pos = (firePoint != null) ? firePoint.position : transform.position;
            Quaternion rot = Quaternion.LookRotation(shooting.AimDirection, Vector3.up);

            GameObject bullet_obj = Instantiate(bulletPrefab, pos, rot);
            BulletMovement bullet = 
                bullet_obj.GetComponent<BulletMovement>();

            // set owner tag of the gun
            if (bullet != null)
            {
                bullet.SetDamage(effective_damage);
                // pass the Player parent so the bullet ignores all
                // player colliders (body, pickup triggers, etc.)
                GameObject player_root = GameObject.Find("Player");

                if (player_root != null)
                    bullet.Initialize(player_root);
                else
                    bullet.Initialize(gameObject);
            }

            // publish gunshot event for guards to hear, and push player pos
            EventBus.Publish(new NoiseWaveEvent
            {
                origin = pos,
                radius = noise_range,
                is_gunshot = true
            });
        }
    }
}
