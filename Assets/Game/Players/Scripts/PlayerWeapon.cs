using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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
    private AudioSource audioSource;
    private PlayerSounds playerSounds;

    [Header("Buff de Daño")]
    [Tooltip("Duracion del buff de daño doble en segundos")]
    public float duracionBuffDano = 5f;
    [Tooltip("Multiplicador de daño durante el buff")]
    public float multiplicadorDano = 2f;

    // Control interno del buff
    private bool buffActivo = false;
    private float danoOriginal;
    private Coroutine buffCoroutine;

    // Propiedad para saber si el buff está activo
    public bool BuffDanoActivo => buffActivo;

    void Awake()
    {
        firePoint = GameObject.Find("FirePoint").transform;
        playerControls = new PlayerControls();
        playerInput = GetComponent<PlayerInput>();
        fireAction = playerControls.Controls.Fire;
        reloadAction = playerControls.Controls.Reload;
        switchWeaponAction = playerControls.Controls.SwitchGun;
        audioSource = GetComponent<AudioSource>();
        playerSounds = GetComponent<PlayerSounds>();
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
        audioSource.PlayOneShot(playerSounds.shot);
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
        // Si hay buff activo al cambiar de arma, cancelarlo
        if (buffActivo)
        {
            CancelarBuffDano();
        }

        weaponData = newWeaponData;
        SetFireMode(newFireMode);
        currentAmmo = weaponData.magSize;
        maxAmmo = weaponData.magSize;
        Debug.Log("Upgraded to weapon: " + weaponData.weaponName);
    }

    #region Buff de Daño

    /// <summary>
    /// Activa el buff de daño doble por la duración configurada.
    /// Llamar desde otros scripts: playerWeapon.ActivarBuffDano();
    /// </summary>
    public void ActivarBuffDano()
    {
        ActivarBuffDano(duracionBuffDano, multiplicadorDano);
    }

    /// <summary>
    /// Activa el buff de daño con duración y multiplicador personalizados.
    /// </summary>
    public void ActivarBuffDano(float duracion, float multiplicador)
    {
        if (weaponData == null) return;

        // Si ya hay un buff activo, cancelarlo primero
        if (buffActivo)
        {
            CancelarBuffDano();
        }

        buffCoroutine = StartCoroutine(BuffDanoCoroutine(duracion, multiplicador));
    }

    private IEnumerator BuffDanoCoroutine(float duracion, float multiplicador)
    {
        // Guardar daño original y aplicar buff
        danoOriginal = weaponData.damage;
        weaponData.damage = danoOriginal * multiplicador;
        buffActivo = true;

        Debug.Log($"Buff de daño activado: {danoOriginal} -> {weaponData.damage} por {duracion} segundos");

        // Esperar la duración
        yield return new WaitForSeconds(duracion);

        // Restaurar daño original
        RestaurarDanoOriginal();
    }

    private void RestaurarDanoOriginal()
    {
        if (weaponData != null && buffActivo)
        {
            weaponData.damage = danoOriginal;
            Debug.Log($"Buff de daño terminado. Daño restaurado a: {weaponData.damage}");
        }
        buffActivo = false;
        buffCoroutine = null;
    }

    /// <summary>
    /// Cancela el buff de daño inmediatamente
    /// </summary>
    public void CancelarBuffDano()
    {
        if (buffCoroutine != null)
        {
            StopCoroutine(buffCoroutine);
            buffCoroutine = null;
        }
        RestaurarDanoOriginal();
    }

    /// <summary>
    /// Obtiene el tiempo restante del buff (aproximado)
    /// </summary>
    public float ObtenerTiempoRestanteBuff()
    {
        // Nota: Para un timer preciso, necesitarías trackear el tiempo de inicio
        return buffActivo ? duracionBuffDano : 0f;
    }

    #endregion
}