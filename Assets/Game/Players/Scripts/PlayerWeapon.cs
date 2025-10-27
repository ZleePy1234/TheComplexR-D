using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerWeapon : MonoBehaviour
{
    public WeaponData weaponData;
    public Transform firePoint;

    public int currentAmmo;
    public int maxAmmo;

    private IFireMode fireMode;
    public float nextTimeToFire;

    private PlayerInput playerInput;
    private PlayerControls playerControls;
    InputAction fireAction;
    InputAction reloadAction;
    InputAction switchWeaponAction;

    void Awake()
    {
        firePoint = GameObject.Find("FirePoint").transform;
        playerControls = new PlayerControls();
        playerInput = GetComponent<PlayerInput>();
        fireAction = playerControls.Controls.Fire;
        reloadAction = playerControls.Controls.Reload;
        switchWeaponAction = playerControls.Controls.SwitchGun;
        
    }
    private void OnEnable()
    {
        playerControls.Enable();
    }
    private void OnDisable()
    {
        playerControls.Disable();
    }
    void Start()
    {
        SetFireMode(new SingleShotMode());
    }

    void Update()
    {
        if (fireAction.IsPressed() && Time.time >= nextTimeToFire && currentAmmo > 0)
        {
            Fire();
        }
        if (reloadAction.WasPressedThisFrame())
        {
            Reload();
        }

        // Use the new Input System switch action to cycle weapons
        if (switchWeaponAction.WasPressedThisFrame())
        {
            var upgrades = GetComponent<WeaponUpgrades>();
            if (upgrades != null)
            {
                upgrades.CycleWeapon();
            }
        }
    }

    public void Fire()
    {
        nextTimeToFire = Time.time + 1f / weaponData.fireRate;
        fireMode.Fire(firePoint, weaponData);
        Debug.Log("Fired weapon: " + weaponData.weaponName);
        currentAmmo--;
    }
    public void Reload()
    {
        currentAmmo = maxAmmo;
        Debug.Log("Reloaded weapon: " + weaponData.weaponName);
    }

    public void SetFireMode(IFireMode newMode)
    {
        fireMode = newMode;
    }

    public void Upgrade(WeaponData newWeaponData, IFireMode newFireMode)
    {
        weaponData = newWeaponData;
        SetFireMode(newFireMode);
        currentAmmo = weaponData.magSize;
        maxAmmo = weaponData.magSize;
        Debug.Log("Upgraded to weapon: " + weaponData.weaponName);
    }
}
