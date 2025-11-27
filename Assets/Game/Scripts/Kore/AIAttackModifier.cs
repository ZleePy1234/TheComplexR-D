using UnityEngine;

/// <summary>
/// Extensión para AIAttackSystem que añade métodos para modificar stats.
/// Agregar este script al mismo GameObject que tiene AIAttackSystem.
/// </summary>
[RequireComponent(typeof(AIAttackSystem))]
public class AIAttackModifier : MonoBehaviour
{
    [Header("Modificadores de Daño")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Modificadores de Velocidad de Ataque")]
    [SerializeField] private float baseAttackRate = 1f;
    [SerializeField] private float attackRateMultiplier = 1f;

    [Header("Modificadores de Rango")]
    [SerializeField] private float baseAttackRange = 10f;
    [SerializeField] private float rangeMultiplier = 1f;

    private AIAttackSystem attackSystem;
    private bool initialized = false;

    // Campos de reflexión cacheados
    private System.Reflection.FieldInfo fieldDamage;
    private System.Reflection.FieldInfo fieldAttackRate;
    private System.Reflection.FieldInfo fieldAttackRange;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (initialized) return;

        attackSystem = GetComponent<AIAttackSystem>();

        if (attackSystem == null)
        {
            Debug.LogError($"{gameObject.name}: No se encontró AIAttackSystem!");
            return;
        }

        // Cachear campos de reflexión
        var bindingFlags = System.Reflection.BindingFlags.NonPublic |
                          System.Reflection.BindingFlags.Instance;

        fieldDamage = typeof(AIAttackSystem).GetField("attackDamage", bindingFlags);
        fieldAttackRate = typeof(AIAttackSystem).GetField("attackRate", bindingFlags);
        fieldAttackRange = typeof(AIAttackSystem).GetField("attackRange", bindingFlags);

        // Leer valores base actuales
        if (fieldDamage != null)
            baseDamage = (float)fieldDamage.GetValue(attackSystem);

        if (fieldAttackRate != null)
            baseAttackRate = (float)fieldAttackRate.GetValue(attackSystem);

        if (fieldAttackRange != null)
            baseAttackRange = (float)fieldAttackRange.GetValue(attackSystem);

        initialized = true;
    }

    #region Modificadores de Daño

    /// <summary>
    /// Establece el multiplicador de daño
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        if (!initialized) Initialize();

        damageMultiplier = Mathf.Max(0.1f, multiplier);
        ApplyDamage();
    }

    /// <summary>
    /// Incrementa el multiplicador de daño
    /// </summary>
    public void IncrementDamageMultiplier(float increment)
    {
        SetDamageMultiplier(damageMultiplier + increment);
    }

    /// <summary>
    /// Establece el daño base
    /// </summary>
    public void SetBaseDamage(float newBaseDamage)
    {
        if (!initialized) Initialize();

        baseDamage = Mathf.Max(0f, newBaseDamage);
        ApplyDamage();
    }

    private void ApplyDamage()
    {
        if (fieldDamage != null && attackSystem != null)
        {
            float newDamage = baseDamage * damageMultiplier;
            fieldDamage.SetValue(attackSystem, newDamage);
            Debug.Log($"{gameObject.name}: Daño actualizado a {newDamage} (base: {baseDamage}, mult: {damageMultiplier:F2})");
        }
    }

    public float GetCurrentDamage() => baseDamage * damageMultiplier;
    public float GetBaseDamage() => baseDamage;
    public float GetDamageMultiplier() => damageMultiplier;

    #endregion

    #region Modificadores de Velocidad de Ataque

    /// <summary>
    /// Establece el multiplicador de velocidad de ataque
    /// </summary>
    public void SetAttackRateMultiplier(float multiplier)
    {
        if (!initialized) Initialize();

        attackRateMultiplier = Mathf.Max(0.1f, multiplier);
        ApplyAttackRate();
    }

    /// <summary>
    /// Incrementa el multiplicador de velocidad de ataque
    /// </summary>
    public void IncrementAttackRateMultiplier(float increment)
    {
        SetAttackRateMultiplier(attackRateMultiplier + increment);
    }

    private void ApplyAttackRate()
    {
        if (fieldAttackRate != null && attackSystem != null)
        {
            float newRate = baseAttackRate * attackRateMultiplier;
            fieldAttackRate.SetValue(attackSystem, newRate);
            Debug.Log($"{gameObject.name}: Velocidad de ataque actualizada a {newRate}");
        }
    }

    public float GetCurrentAttackRate() => baseAttackRate * attackRateMultiplier;

    #endregion

    #region Modificadores de Rango

    /// <summary>
    /// Establece el multiplicador de rango
    /// </summary>
    public void SetRangeMultiplier(float multiplier)
    {
        if (!initialized) Initialize();

        rangeMultiplier = Mathf.Max(0.1f, multiplier);
        ApplyRange();
    }

    private void ApplyRange()
    {
        if (fieldAttackRange != null && attackSystem != null)
        {
            float newRange = baseAttackRange * rangeMultiplier;
            fieldAttackRange.SetValue(attackSystem, newRange);
            Debug.Log($"{gameObject.name}: Rango de ataque actualizado a {newRange}");
        }
    }

    public float GetCurrentRange() => baseAttackRange * rangeMultiplier;

    #endregion

    #region Aplicar Todos los Modificadores

    /// <summary>
    /// Aplica todos los modificadores actuales
    /// </summary>
    public void ApplyAllModifiers()
    {
        if (!initialized) Initialize();

        ApplyDamage();
        ApplyAttackRate();
        ApplyRange();
    }

    /// <summary>
    /// Reinicia todos los modificadores a 1
    /// </summary>
    public void ResetAllModifiers()
    {
        damageMultiplier = 1f;
        attackRateMultiplier = 1f;
        rangeMultiplier = 1f;
        ApplyAllModifiers();
    }

    #endregion
}