using UnityEngine;
using static GameEvents;

public class Rifle : MonoBehaviour
{
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private int ammo = -1;

    private float _nextFireTime;

    private void Update()
    {
        GameObject bullet_obj;

        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime && ammo != 0)
        {
            if (ammo > 0) ammo--;
            _nextFireTime = Time.time + fireRate;

            Vector3 pos = (firePoint != null) ? firePoint.position : transform.position;
            Quaternion rot = Quaternion.LookRotation(shooting.AimDirection, Vector3.up);
            bullet_obj = Instantiate(bulletPrefab, pos, rot);

            BulletMovement bullet =
                bullet_obj.GetComponent<BulletMovement>();

            // set owner tag of the gun
            if (bullet_obj != null)
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
            EventBus.Publish(new GunshotEvent { player_position = pos });
        }
    }
}
