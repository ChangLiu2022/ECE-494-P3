using UnityEngine;
using static GameEvents;
using static GunEvents;

public class InvPistol : MonoBehaviour
{
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.3f;

    [SerializeField] private LayerMask wallLayer;
    
    private float _nextFireTime;
    private HasInventory inv;

    // sets the HasInventory component to use for ammo verification
    private void Awake()
    {
        GameObject player = GameObject.Find("Player");

        if (player != null)
        {
            inv = player.GetComponent<HasInventory>();

            if (inv == null)
            {
                Debug.LogError("Player found but HasInventory component is missing.");
            }
        }
        else
        {
            Debug.LogError("No GameObject with name 'Player' found in the scene.");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextFireTime)
        {
            if (Physics.CheckSphere(firePoint.position, 0.1f, wallLayer))
            {
                Debug.Log("FirePoint is inside a wall, skipping shot");
                return;
            }
            
            if (inv != null && inv.BulletCount <= 0)
            {
                EventBus.Publish(new FailedToFireEvent()); // to trigger HUD flashing
                Debug.Log("Not enough bullets to fire!");
                return;
            }

            if (inv != null) inv.AddBullets(-1); // update bullet count

            _nextFireTime = Time.time + fireRate;

            Vector3 pos = (firePoint != null) ? firePoint.position : transform.position;
            Quaternion rot = Quaternion.LookRotation(shooting.AimDirection, Vector3.up);

            GameObject bullet_obj = Instantiate(bulletPrefab, pos, rot);
            BulletMovement bullet = 
                bullet_obj.GetComponent<BulletMovement>();

            // set owner tag of the gun
            if (bullet != null)
            {
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
                radius = 5f,
                is_gunshot = true
            });
        }
    }
}
