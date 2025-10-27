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

    // index to track current weapon for cycling
    private int currentIndex = 0;

    void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();
        currentIndex = 0;
        SetPistol();
    }

    // Removed Input.GetKeyDown(H) here; cycling is done from PlayerWeapon via CycleWeapon()

    // public method so PlayerWeapon can trigger cycling using the InputAction
    public void CycleWeapon()
    {
        if (weaponDataList == null || weaponDataList.Count == 0) return;
        currentIndex = (currentIndex + 1) % weaponDataList.Count;
        SwitchToCurrent();
    }

    // use the existing Set... methods for each index
    private void SwitchToCurrent()
    {
        switch (currentIndex)
        {
            case 0: SetPistol(); break;
            case 1: SetHandCannon(); break;
            case 2: SetMachinePistol(); break;
            case 3: SetSmg(); break;
            case 4: SetShotgun(); break;
            default: SetPistol(); break;
        }
    }

    [ProButton] public void SetPistol()
    {
        playerWeapon.Upgrade(weaponDataList[0], new SingleShotSpreadMode());
    }
    [ProButton] public void SetHandCannon()
    {
        playerWeapon.Upgrade(weaponDataList[1], new SingleShotMode());
    }
    [ProButton] public void SetMachinePistol()
    {
        playerWeapon.Upgrade(weaponDataList[2], new SingleShotSpreadMode());
    }
    [ProButton] public void SetSmg()
    {
        playerWeapon.Upgrade(weaponDataList[3], new SingleShotSpreadMode());
    }
    [ProButton] public void SetShotgun()
    {
        playerWeapon.Upgrade(weaponDataList[4], new ShotgunSpreadMode());
    }
}
