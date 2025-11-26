using UnityEngine;

public class PlayerSounds : MonoBehaviour
{
    public AudioClip[] gunSounds;
    [HideInInspector]public AudioClip shot;

    private PlayerWeapon playerWeapon;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
    }

    // Update is called once per frame
    void Update()
    {
        if(playerWeapon.weaponData == null)
        {
            return;
        }
        else if(playerWeapon.weaponData.weaponName == "P-09")
        {
            shot = gunSounds[0];
        }
        else if(playerWeapon.weaponData.weaponName == "HC-09")
        {
            shot = gunSounds[1];
        }
        else if(playerWeapon.weaponData.weaponName == "MP-09")
        {
            shot = gunSounds[2];
        }
        else if(playerWeapon.weaponData.weaponName == "SMG-09")
        {
            shot = gunSounds[3];
        }
        else if(playerWeapon.weaponData.weaponName == "SG-09")
        {
            shot = gunSounds[4];
        }
    }
}
