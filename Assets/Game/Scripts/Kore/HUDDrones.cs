using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD simple para drones:
/// - Texto nombre del tipo de dron
/// - Texto cantidad de drones
/// - Fill de cooldown de habilidad
/// </summary>
public class HUDDrones : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Drones sistemaDrones;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoTipoDrone;
    [SerializeField] private TextMeshProUGUI textoCantidadDrones;
    [SerializeField] private Image fillCooldown;

    void Start()
    {
        if (sistemaDrones == null)
            sistemaDrones = FindFirstObjectByType<Drones>();
    }

    void Update()
    {
        if (sistemaDrones == null) return;

        ActualizarTipoDrone();
        ActualizarCantidadDrones();
        ActualizarCooldown();
    }

    void ActualizarTipoDrone()
    {
        if (textoTipoDrone == null) return;

        Drones.TipoDron tipoActual = sistemaDrones.ObtenerTipoDronActual();

        switch (tipoActual)
        {
            case Drones.TipoDron.Buscador:
                textoTipoDrone.text = "BUSCADOR";
                break;
            case Drones.TipoDron.Atacante:
                textoTipoDrone.text = "ATACANTE";
                break;
            case Drones.TipoDron.Defensor:
                textoTipoDrone.text = "DEFENSOR";
                break;
        }
    }

    void ActualizarCantidadDrones()
    {
        if (textoCantidadDrones == null) return;

        int cantidadActivos = sistemaDrones.ObtenerCantidadDronesActivos();
        int cantidadTotal = sistemaDrones.ObtenerTotalDronesEnLista();

        textoCantidadDrones.text = $"{cantidadActivos}/{cantidadTotal}";
    }

    void ActualizarCooldown()
    {
        if (fillCooldown == null) return;

        Drones.TipoDron tipoActual = sistemaDrones.ObtenerTipoDronActual();
        float progreso = sistemaDrones.ObtenerProgresoCooldown(tipoActual);

        fillCooldown.fillAmount = progreso;
    }
}