using UnityEngine;

public class Shotgun : MonoBehaviour
{
    [SerializeField] private PlayerShooting shooting;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.6f;
    [SerializeField] private int ammo = -1;
    [SerializeField] private int pelletCount = 5;
    [SerializeField] private float spreadAngle = 30f;

    private float _nextFireTime;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextFireTime && ammo != 0)
        {
            if (ammo > 0) ammo--;
            _nextFireTime = Time.time + fireRate;

            Vector3 pos = (firePoint != null) ? firePoint.position : transform.position;
            float halfSpread = spreadAngle / 2f;

            for (int i = 0; i < pelletCount; i++)
            {
                float angle = Mathf.Lerp(-halfSpread, halfSpread, (float)i / (pelletCount - 1));
                Quaternion spread = Quaternion.AngleAxis(angle, Vector3.up);
                Quaternion rot = Quaternion.LookRotation(spread * shooting.AimDirection, Vector3.up);
                Instantiate(bulletPrefab, pos, rot);
            }
        }
    }
}
