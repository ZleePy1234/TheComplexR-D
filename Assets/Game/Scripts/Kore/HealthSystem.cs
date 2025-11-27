using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de vida para cualquier entidad del juego.
/// Incluye regeneración, eventos y métodos para modificar stats desde la tienda.
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Configuración de Muerte")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0f;

    [Header("Regeneración de Vida")]
    [SerializeField] private bool enableRegeneration = false;
    [SerializeField] private string[] tagsToRegenerate;
    [SerializeField] private float regenerationRate = 5f;
    [SerializeField] private float regenerationDelay = 3f;
    private float timeSinceLastDamage = 0f;
    private bool canRegenerate = false;

    [Header("Modificadores de Stats")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float healthMultiplier = 1f;

    [Header("Eventos")]
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;
    public UnityEvent OnRevive;
    public UnityEvent<float> OnMaxHealthChanged;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public float HealthMultiplier => healthMultiplier;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;
        IsDead = false;
        CheckIfCanRegenerate();
    }

    private void Update()
    {
        if (!IsDead && canRegenerate && currentHealth < maxHealth)
        {
            timeSinceLastDamage += Time.deltaTime;

            if (timeSinceLastDamage >= regenerationDelay)
            {
                Regenerate();
            }
        }
    }

    #region Métodos de Modificación de Stats (Para Tienda)

    /// <summary>
    /// Establece el multiplicador de vida máxima
    /// </summary>
    public void SetHealthMultiplier(float multiplier)
    {
        healthMultiplier = Mathf.Max(0.1f, multiplier);
        RecalcularVidaMaxima();
    }

    /// <summary>
    /// Incrementa el multiplicador de vida
    /// </summary>
    public void IncrementHealthMultiplier(float increment)
    {
        SetHealthMultiplier(healthMultiplier + increment);
    }

    /// <summary>
    /// Establece la vida máxima base
    /// </summary>
    public void SetBaseMaxHealth(float newBaseHealth)
    {
        baseMaxHealth = Mathf.Max(1f, newBaseHealth);
        RecalcularVidaMaxima();
    }

    private void RecalcularVidaMaxima()
    {
        float oldMaxHealth = maxHealth;
        maxHealth = baseMaxHealth * healthMultiplier;

        // Mantener el mismo porcentaje de vida
        float healthPercentage = oldMaxHealth > 0 ? currentHealth / oldMaxHealth : 1f;
        currentHealth = maxHealth * healthPercentage;

        OnMaxHealthChanged?.Invoke(maxHealth);
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"{gameObject.name}: Vida máxima actualizada a {maxHealth} (base: {baseMaxHealth}, multiplicador: {healthMultiplier:F2})");
    }

    /// <summary>
    /// Obtiene la vida máxima base (sin multiplicadores)
    /// </summary>
    public float GetBaseMaxHealth() => baseMaxHealth;

    #endregion

    #region Regeneración

    /// Verifica si este objeto tiene un tag que permite regeneración
    private void CheckIfCanRegenerate()
    {
        if (!enableRegeneration || tagsToRegenerate == null || tagsToRegenerate.Length == 0)
        {
            canRegenerate = false;
            return;
        }

        foreach (string tag in tagsToRegenerate)
        {
            if (gameObject.CompareTag(tag))
            {
                canRegenerate = true;
                return;
            }
        }

        canRegenerate = false;
    }

    /// Regenera vida gradualmente    
    private void Regenerate()
    {
        float amountToHeal = regenerationRate * Time.deltaTime;
        currentHealth += amountToHeal;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);
    }

    #endregion

    #region Daño y Curación

    /// Recibe daño y reduce la vida actual    
    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (damage <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        timeSinceLastDamage = 0f;

        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"{gameObject.name} recibió {damage} de daño. Vida restante: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// Cura la unidad
    public void Heal(float amount)
    {
        if (IsDead) return;
        if (amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"{gameObject.name} curado por {amount}. Vida actual: {currentHealth}/{maxHealth}");
    }

    /// Establece la vida al máximo    
    public void FullHeal()
    {
        if (IsDead) return;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    #endregion

    #region Muerte y Resurrección

    /// Ejecuta la muerte de la unidad    
    private void Die()
    {
        if (IsDead) return;

        IsDead = true;
        OnDeath?.Invoke();

        Debug.Log($"{gameObject.name} ha muerto");

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    /// Mata instantáneamente a la unidad
    public void Kill()
    {
        TakeDamage(currentHealth);
    }

    /// Revive la unidad con vida completa
    public void Revive()
    {
        IsDead = false;
        currentHealth = maxHealth;
        timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(currentHealth);
        OnRevive?.Invoke();

        Debug.Log($"{gameObject.name} ha sido revivido");
    }

    /// Revive la unidad con un porcentaje específico de vida
    public void Revive(float healthPercentage)
    {
        IsDead = false;
        currentHealth = maxHealth * Mathf.Clamp01(healthPercentage);
        timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(currentHealth);
        OnRevive?.Invoke();

        Debug.Log($"{gameObject.name} ha sido revivido con {healthPercentage * 100}% de vida");
    }

    #endregion

    #region Utilidades

    /// <summary>
    /// Obtiene el porcentaje de vida como string formateado
    /// </summary>
    public string GetHealthString()
    {
        return $"{currentHealth:F0}/{maxHealth:F0}";
    }

    /// <summary>
    /// Verifica si está a vida completa
    /// </summary>
    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    /// <summary>
    /// Verifica si la vida está baja (menor al porcentaje especificado)
    /// </summary>
    public bool IsLowHealth(float percentage = 0.25f)
    {
        return HealthPercentage <= percentage;
    }

    #endregion
}