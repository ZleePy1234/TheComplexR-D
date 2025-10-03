using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerWeapon : MonoBehaviour
{
    public WeaponData weaponData;
    public Transform firePoint;

    public int currentAmmo;

    private IFireMode fireMode;
    public float nextTimeToFire;

    private PlayerInput playerInput;
    private PlayerControls playerControls;
    InputAction fireAction;

    void Awake()
    {
        firePoint = GameObject.Find("FirePoint").transform;
        playerControls = new PlayerControls();
        playerInput = GetComponent<PlayerInput>();
        fireAction = playerControls.Controls.Fire;
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
        if (fireAction.IsPressed() && Time.time >= nextTimeToFire)
        {
            Fire();
        }
    }

    public void Fire()
    {
        nextTimeToFire = Time.time + 1f / weaponData.fireRate;
        fireMode.Fire(firePoint, weaponData);
        Debug.Log("Fired weapon: " + weaponData.weaponName);
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
        Debug.Log("Upgraded to weapon: " + weaponData.weaponName);
    }
}
