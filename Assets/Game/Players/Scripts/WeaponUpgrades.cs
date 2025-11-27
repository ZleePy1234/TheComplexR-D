using UnityEngine;
using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;

/// <summary>
/// Sistema de armas del jugador.
/// - No hay opción de ciclar armas
/// - Cuando compras un arma, se equipa automáticamente
/// - La tienda solo muestra armas diferentes a la equipada
/// </summary>
[RequireComponent(typeof(PlayerWeapon))]
public class WeaponUpgrades : MonoBehaviour
{
    [Header("WeaponData List")]
    public List<WeaponData> weaponDataList;

    [Header("Arma Actual")]
    [SerializeField] private int indiceArmaActual = 0;

    private PlayerWeapon playerWeapon;

    void Awake()
    {
        playerWeapon = GetComponent<PlayerWeapon>();

        // Equipar la pistola por defecto
        EquiparArma(0);
    }

    /// <summary>
    /// Cicla entre todas las armas (para pruebas)
    /// </summary>
    public void CycleWeapon()
    {
        if (weaponDataList == null || weaponDataList.Count == 0) return;

        indiceArmaActual = (indiceArmaActual + 1) % weaponDataList.Count;
        EquiparArmaPorIndice(indiceArmaActual);
    }

    /// <summary>
    /// Cicla en reversa entre todas las armas
    /// </summary>
    public void CycleWeaponReverse()
    {
        if (weaponDataList == null || weaponDataList.Count == 0) return;

        indiceArmaActual--;
        if (indiceArmaActual < 0) indiceArmaActual = weaponDataList.Count - 1;
        EquiparArmaPorIndice(indiceArmaActual);
    }

    /// <summary>
    /// Equipa un arma por índice (usado internamente)
    /// </summary>
    private void EquiparArmaPorIndice(int indice)
    {
        switch (indice)
        {
            case 0: SetPistol(); break;
            case 1: SetHandCannon(); break;
            case 2: SetMachinePistol(); break;
            case 3: SetSmg(); break;
            case 4: SetShotgun(); break;
            default: SetPistol(); break;
        }
    }

    /// <summary>
    /// Equipa un arma por índice (público, para la tienda)
    /// </summary>
    public void EquiparArma(int indice)
    {
        if (indice < 0 || indice >= weaponDataList.Count)
        {
            Debug.LogWarning($"Índice de arma inválido: {indice}");
            return;
        }

        indiceArmaActual = indice;
        EquiparArmaPorIndice(indice);
        Debug.Log($"Arma equipada: {weaponDataList[indice].weaponName}");
    }

    /// <summary>
    /// Obtiene el índice del arma actualmente equipada
    /// </summary>
    public int GetIndiceArmaActual()
    {
        return indiceArmaActual;
    }

    /// <summary>
    /// Obtiene el nombre del arma actual
    /// </summary>
    public string ObtenerNombreArmaActual()
    {
        if (indiceArmaActual < 0 || indiceArmaActual >= weaponDataList.Count)
            return "Ninguna";

        return weaponDataList[indiceArmaActual].weaponName;
    }

    /// <summary>
    /// Obtiene el WeaponData del arma actual
    /// </summary>
    public WeaponData ObtenerArmaActual()
    {
        if (indiceArmaActual < 0 || indiceArmaActual >= weaponDataList.Count)
            return null;

        return weaponDataList[indiceArmaActual];
    }

    /// <summary>
    /// Verifica si un arma específica está equipada
    /// </summary>
    public bool EstaEquipada(int indice)
    {
        return indiceArmaActual == indice;
    }

    #region Métodos de Armas Específicas

    [ProButton]
    public void SetPistol()
    {
        if (weaponDataList.Count > 0)
        {
            playerWeapon.Upgrade(weaponDataList[0], new SingleShotSpreadMode());
            indiceArmaActual = 0;
        }
    }

    [ProButton]
    public void SetHandCannon()
    {
        if (weaponDataList.Count > 1)
        {
            playerWeapon.Upgrade(weaponDataList[1], new SingleShotMode());
            indiceArmaActual = 1;
        }
    }

    [ProButton]
    public void SetMachinePistol()
    {
        if (weaponDataList.Count > 2)
        {
            playerWeapon.Upgrade(weaponDataList[2], new SingleShotSpreadMode());
            indiceArmaActual = 2;
        }
    }

    [ProButton]
    public void SetSmg()
    {
        if (weaponDataList.Count > 3)
        {
            playerWeapon.Upgrade(weaponDataList[3], new SingleShotSpreadMode());
            indiceArmaActual = 3;
        }
    }

    [ProButton]
    public void SetShotgun()
    {
        if (weaponDataList.Count > 4)
        {
            playerWeapon.Upgrade(weaponDataList[4], new ShotgunSpreadMode());
            indiceArmaActual = 4;
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Debug: Mostrar Arma Actual")]
    void DebugMostrarArma()
    {
        Debug.Log($"Arma actual: {ObtenerNombreArmaActual()} (índice: {indiceArmaActual})");
    }

    #endregion
}