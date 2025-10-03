using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Stats")]
    public string weaponName;
    public float damage;
    public float fireRate;
    public float magSize;
    public float reloadTime;
    
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileLifetime;
    public float projectileSpeed;
    public float projectilesPerShot;
    public float projectileSpread; // in degrees
}
