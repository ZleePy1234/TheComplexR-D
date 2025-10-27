using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDscript : MonoBehaviour
{
    [SerializeField] private int hudHealth;
    [SerializeField] private int hudAmmo;
    [SerializeField] private int hudResin;

    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] TextMeshProUGUI ammoText;
    [SerializeField] TextMeshProUGUI resinText;
    [SerializeField] Image healthBar;
    [SerializeField] Image ammoBar;

    private int maxHealth = 100;
    private int maxAmmo;

    public GameObject player;

    private enum DroneTypes
    {
        Scout,
        Attack,
        Defense
    }
    [SerializeField] private DroneTypes currentDroneType;
    void Update()
    {
        AssignValues();
        NumbersUpdate();
        Fills();
    }

    void AssignValues()
    {
        hudHealth = player.GetComponent<PlayerStats>().currentHealth;
        hudAmmo = player.GetComponent<PlayerWeapon>().currentAmmo;
        hudResin = player.GetComponent<PlayerStats>().playerResin;
        maxAmmo = player.GetComponent<PlayerWeapon>().maxAmmo;
        maxHealth = player.GetComponent<PlayerStats>().maxHealth;
    }

    void NumbersUpdate()
    {
        healthText.text = hudHealth.ToString();
        ammoText.text = hudAmmo.ToString();
        resinText.text = hudResin.ToString();
    }
    void Fills()
    {
        float hpFill = Mathf.Clamp01(hudHealth / maxHealth);
        healthBar.fillAmount = hpFill;
        float ammoFill = Mathf.Clamp01((float)hudAmmo / maxAmmo);
        ammoBar.fillAmount = ammoFill;
    }
}
