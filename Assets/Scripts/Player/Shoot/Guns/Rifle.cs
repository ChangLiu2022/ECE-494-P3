using UnityEngine;

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
        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime && ammo != 0)
        {
            if (ammo > 0) ammo--;
            _nextFireTime = Time.time + fireRate;

            Vector3 pos = (firePoint != null) ? firePoint.position : transform.position;
            Quaternion rot = Quaternion.LookRotation(shooting.AimDirection, Vector3.up);
            Instantiate(bulletPrefab, pos, rot);
        }
    }
}
