using UnityEngine;

public interface IFireMode
{
    void Fire(Transform firePoint, WeaponData data);
}

public class SingleShotMode : IFireMode
{
    public void Fire(Transform firePoint, WeaponData data)
    {
        if (firePoint == null) Debug.LogError("firePoint is null in Fire!");
        if (data == null) Debug.LogError("weaponData is null in Fire!");
        if (data.projectilePrefab == null) Debug.LogError("projectilePrefab is null!");
        GameObject projectile = Object.Instantiate(data.projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("Rigidbody is null on projectile!");
        else rb.linearVelocity = firePoint.forward * data.projectileSpeed;
        Object.Destroy(projectile, data.projectileLifetime);
    }
}

public class BurstFireMode : IFireMode
{
    public void Fire(Transform firePoint, WeaponData data)
    {
        for (int i = 0; i < data.projectilesPerShot; i++)
        {
            GameObject projectile = Object.Instantiate(data.projectilePrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            rb.linearVelocity = firePoint.forward * data.projectileSpeed;
            Object.Destroy(projectile, data.projectileLifetime);
        }
    }
}

public class ShotgunSpreadMode : IFireMode
{
    public void Fire(Transform firePoint, WeaponData data)
    {
        for (int i = 0; i < data.projectilesPerShot; i++)
        {
            // Calculate horizontal spread direction
            float spreadAmount = Random.Range(-data.projectileSpread, data.projectileSpread);
            Vector3 direction = firePoint.forward + firePoint.right * Mathf.Tan(spreadAmount * Mathf.Deg2Rad);
            direction.Normalize();

            GameObject projectile = Object.Instantiate(data.projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            rb.linearVelocity = direction * data.projectileSpeed;
            Object.Destroy(projectile, data.projectileLifetime);
        }
    }
}