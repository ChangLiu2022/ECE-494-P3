using UnityEngine;
using static GameEvents;

public class Shotgun : MonoBehaviour
{
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.6f;
    [SerializeField] private int ammo = -1;
    [SerializeField] private int pelletCount = 5;
    [SerializeField] private float spreadAngle = 30f;
    [SerializeField] private float noise_range = 20f;

    [SerializeField] private LayerMask wallLayer;

    private float _nextFireTime;

    private float base_fire_rate;
    private float effective_fire_rate;
    private int effective_damage;

    private void Awake()
    {
        base_fire_rate = fireRate;
        RefreshStats();
    }

    private void OnEnable() => EventBus.Subscribe<UpgradePurchasedEvent>(OnUpgrade);
    private void OnDisable() => EventBus.Unsubscribe<UpgradePurchasedEvent>(OnUpgrade);

    private void OnUpgrade(UpgradePurchasedEvent e)
    {
        if (e.weapon == GunUpgrades.Weapon.Shotgun) RefreshStats();
    }

    private void RefreshStats()
    {
        effective_fire_rate = base_fire_rate / GunUpgrades.GetFireRateMultiplier(GunUpgrades.Weapon.Shotgun);
        effective_damage = Mathf.Max(1, Mathf.RoundToInt(GunUpgrades.GetDamageMultiplier(GunUpgrades.Weapon.Shotgun)));
    }

    // TODO add shothun bullets to inventory

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextFireTime && ammo != 0)
        {
            if (Physics.CheckSphere(firePoint.position, 0.1f, wallLayer))
            {
                return;
            }
            if (ammo > 0) ammo--;
            _nextFireTime = Time.time + effective_fire_rate;

            Vector3 pos = (firePoint != null) ? firePoint.position : transform.position;
            float halfSpread = spreadAngle / 2f;

            GameObject bullet_obj;

            for (int i = 0; i < pelletCount; i++)
            {
                float angle = Mathf.Lerp(-halfSpread, halfSpread, (float)i / (pelletCount - 1));
                Quaternion spread = Quaternion.AngleAxis(angle, Vector3.up);
                Quaternion rot = Quaternion.LookRotation(spread * shooting.AimDirection, Vector3.up);
                bullet_obj = Instantiate(bulletPrefab, pos, rot);

                BulletMovement bullet =
                    bullet_obj.GetComponent<BulletMovement>();

                // set owner tag of the gun
                if (bullet_obj != null)
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
