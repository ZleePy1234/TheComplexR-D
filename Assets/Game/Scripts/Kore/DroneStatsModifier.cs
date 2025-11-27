using UnityEngine;

/// <summary>
/// Extensión para AIAttackSystem que permite modificar el daño desde la tienda.
/// Agrega este componente a los drones atacantes para poder modificar su daño.
/// </summary>
public class DroneStatsModifier : MonoBehaviour
{
    [Header("Modificadores de Stats")]
    [SerializeField] private float multiplicadorDaño = 1f;
    [SerializeField] private float multiplicadorVida = 1f;

    [Header("Valores Base (se leen automáticamente)")]
    [SerializeField] private float dañoBase = 10f;
    [SerializeField] private float vidaBase = 100f;

    private AIAttackSystem attackSystem;
    private HealthSystem healthSystem;

    private bool initialized = false;

    void Start()
    {
        Inicializar();
    }

    void Inicializar()
    {
        if (initialized) return;

        attackSystem = GetComponent<AIAttackSystem>();
        healthSystem = GetComponent<HealthSystem>();

        // Guardar valores base
        if (attackSystem != null)
        {
            // Intentar leer el daño base actual
            dañoBase = ObtenerDañoActual();
        }

        if (healthSystem != null)
        {
            vidaBase = healthSystem.MaxHealth;
        }

        initialized = true;
    }

    /// <summary>
    /// Establece el multiplicador de daño y lo aplica
    /// </summary>
    public void SetMultiplicadorDaño(float multiplicador)
    {
        if (!initialized) Inicializar();

        multiplicadorDaño = multiplicador;
        AplicarDaño();
    }

    /// <summary>
    /// Establece el multiplicador de vida y lo aplica
    /// </summary>
    public void SetMultiplicadorVida(float multiplicador)
    {
        if (!initialized) Inicializar();

        multiplicadorVida = multiplicador;
        AplicarVida();
    }

    /// <summary>
    /// Incrementa el multiplicador de daño
    /// </summary>
    public void IncrementarDaño(float incremento)
    {
        SetMultiplicadorDaño(multiplicadorDaño + incremento);
    }

    /// <summary>
    /// Incrementa el multiplicador de vida
    /// </summary>
    public void IncrementarVida(float incremento)
    {
        SetMultiplicadorVida(multiplicadorVida + incremento);
    }

    void AplicarDaño()
    {
        if (attackSystem == null) return;

        float nuevoDaño = dañoBase * multiplicadorDaño;

        // Usar reflexión como fallback si no hay método público
        var field = typeof(AIAttackSystem).GetField("attackDamage",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(attackSystem, nuevoDaño);
            Debug.Log($"{gameObject.name}: Daño actualizado a {nuevoDaño}");
        }
    }

    void AplicarVida()
    {
        if (healthSystem == null) return;

        float nuevaVida = vidaBase * multiplicadorVida;

        // Usar reflexión para maxHealth
        var fieldMax = typeof(HealthSystem).GetField("maxHealth",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (fieldMax != null)
        {
            fieldMax.SetValue(healthSystem, nuevaVida);

            // También curar al drone
            healthSystem.FullHeal();

            Debug.Log($"{gameObject.name}: Vida máxima actualizada a {nuevaVida}");
        }
    }

    float ObtenerDañoActual()
    {
        if (attackSystem == null) return 10f;

        var field = typeof(AIAttackSystem).GetField("attackDamage",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            return (float)field.GetValue(attackSystem);
        }

        return 10f;
    }

    public float GetMultiplicadorDaño() => multiplicadorDaño;
    public float GetMultiplicadorVida() => multiplicadorVida;
    public float GetDañoActual() => dañoBase * multiplicadorDaño;
    public float GetVidaActual() => vidaBase * multiplicadorVida;
}