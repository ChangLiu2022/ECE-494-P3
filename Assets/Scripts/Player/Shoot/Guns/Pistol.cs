using UnityEngine;

public class Pistol : MonoBehaviour
{
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private int ammo = -1;
    [SerializeField] private HasInventory inventory;

    private float _nextFireTime;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextFireTime && ammo != 0)
        {
            inventory.Shoot(1);
            if (ammo > 0) ammo--;
            _nextFireTime = Time.time + fireRate;

            Vector3 pos = (firePoint != null) ? firePoint.position : transform.position;
            Quaternion rot = Quaternion.LookRotation(shooting.AimDirection, Vector3.up);
            Instantiate(bulletPrefab, pos, rot);
        }
    }
}
