using UnityEngine;
using System.Collections.Generic;
using System;
using com.cyborgAssets.inspectorButtonPro;

[RequireComponent(typeof(PlayerWeapon))]
public class WeaponUpgrades : MonoBehaviour
{
    [Header("WeaponData List")]

    private PlayerWeapon playerWeapon;
    public List<WeaponData> weaponDataList;

    void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
    }
    [ProButton] public void SetPistol()
    {
        playerWeapon.Upgrade(weaponDataList[0], new SingleShotMode());
    }
    [ProButton] public void SetHandCannon()
    {
        playerWeapon.Upgrade(weaponDataList[1], new SingleShotMode());
    }
    [ProButton] public void SetMachinePistol()
    {
        playerWeapon.Upgrade(weaponDataList[2], new SingleShotMode());
    }
    [ProButton] public void SetSmg()
    {
        playerWeapon.Upgrade(weaponDataList[3], new SingleShotMode());
    }
    [ProButton] public void SetShotgun()
    {
        playerWeapon.Upgrade(weaponDataList[4], new ShotgunSpreadMode());
    }
}
