using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de vida para cualquier entidad del juego.
/// Incluye regeneraci�n, eventos y m�todos para modificar stats desde la tienda.
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Configuraci�n de Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Configuraci�n de Muerte")]
    [SerializeField] private bool destroyOnDeath = true;
    [Tooltip("Delay antes de iniciar el proceso de muerte (para sincronizar con animaciones)")]
    [SerializeField] private float deathAnimationDelay = 0f;
    [Tooltip("Delay adicional despu�s de la animaci�n antes de destruir el GameObject")]
    [SerializeField] private float destroyDelay = 0f;
    [Tooltip("Si es true, dispara el evento OnDeath inmediatamente. Si es false, lo dispara despu�s del deathAnimationDelay")]
    [SerializeField] private bool fireDeathEventImmediately = true;

    [Header("Regeneraci�n de Vida")]
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
    [Tooltip("Se dispara cuando inicia la secuencia de muerte (antes del delay de animaci�n)")]
    public UnityEvent OnDeathSequenceStarted;
    [Tooltip("Se dispara justo antes de destruir el GameObject")]
    public UnityEvent OnAboutToDestroy;
    public UnityEvent OnRevive;
    public UnityEvent<float> OnMaxHealthChanged;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }
    public bool IsInDeathSequence { get; private set; }
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public float HealthMultiplier => healthMultiplier;
    public float DeathAnimationDelay => deathAnimationDelay;
    public float DestroyDelay => destroyDelay;

    // Control de coroutine de muerte
    private Coroutine deathCoroutine;

    private void Awake()
    {
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;
        IsDead = false;
        IsInDeathSequence = false;
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

    #region M�todos de Modificaci�n de Stats (Para Tienda)

    /// <summary>
    /// Establece el multiplicador de vida m�xima
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
    /// Establece la vida m�xima base
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

        Debug.Log($"{gameObject.name}: Vida m�xima actualizada a {maxHealth} (base: {baseMaxHealth}, multiplicador: {healthMultiplier:F2})");
    }

    /// <summary>
    /// Obtiene la vida m�xima base (sin multiplicadores)
    /// </summary>
    public float GetBaseMaxHealth() => baseMaxHealth;

    #endregion

    #region Regeneraci�n

    /// Verifica si este objeto tiene un tag que permite regeneraci�n
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

    #region Da�o y Curaci�n

    /// Recibe da�o y reduce la vida actual    
    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (damage <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        timeSinceLastDamage = 0f;

        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"{gameObject.name} recibi� {damage} de da�o. Vida restante: {currentHealth}/{maxHealth}");

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

    /// Establece la vida al m�ximo    
    public void FullHeal()
    {
        if (IsDead) return;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    #endregion

    #region Muerte y Resurrecci�n

    /// Ejecuta la muerte de la unidad    
    private void Die()
    {
        if (IsDead) return;
        
        IsDead = true;
        IsInDeathSequence = true;

        Debug.Log($"{gameObject.name} ha muerto");

        // Siempre disparar el evento de inicio de secuencia de muerte
        OnDeathSequenceStarted?.Invoke();

        // Si hay delay de animaci�n, usar coroutine
        if (deathAnimationDelay > 0f || destroyDelay > 0f)
        {
            // Si est� configurado para disparar inmediatamente
            if (fireDeathEventImmediately)
            {
                OnDeath?.Invoke();
            }

            deathCoroutine = StartCoroutine(DeathSequence());
        }
        else
        {
            // Sin delays, ejecutar inmediatamente
            OnDeath?.Invoke();

            if (destroyOnDeath)
            {
                OnAboutToDestroy?.Invoke();
                
                Destroy(gameObject);
            }

            IsInDeathSequence = false;
        }
    }

    private IEnumerator DeathSequence()
    {
        GameManager gameManager = GameObject.FindWithTag("EndManager")?.GetComponent<GameManager>();
        // Esperar el delay de animaci�n de muerte
        if (deathAnimationDelay > 0f)
        {
            yield return new WaitForSeconds(deathAnimationDelay);
        }

        // Disparar evento de muerte despu�s del delay si est� configurado as�
        if (!fireDeathEventImmediately)
        {
            OnDeath?.Invoke();
        }

        // Esperar el delay adicional antes de destruir
        if (destroyDelay > 0f)
        {
            yield return new WaitForSeconds(destroyDelay);
        }

        // Destruir el GameObject si est� configurado
        if (destroyOnDeath)
        {
            OnAboutToDestroy?.Invoke();
            Destroy(gameObject);
            gameManager.enemiesKilled++;
        }

        IsInDeathSequence = false;
        deathCoroutine = null;
    }

    /// Mata instant�neamente a la unidad
    public void Kill()
    {
        TakeDamage(currentHealth);
    }

    /// <summary>
    /// Mata instant�neamente sin delays (�til para limpiar escenas)
    /// </summary>
    public void KillImmediate()
    {
        if (IsDead) return;

        // Cancelar cualquier secuencia de muerte en progreso
        CancelDeathSequence();

        IsDead = true;
        currentHealth = 0;

        OnDeath?.Invoke();

        if (destroyOnDeath)
        {
            OnAboutToDestroy?.Invoke();
            Destroy(gameObject);
        }
    }

    /// Revive la unidad con vida completa
    public void Revive()
    {
        // Cancelar cualquier secuencia de muerte en progreso
        CancelDeathSequence();

        IsDead = false;
        IsInDeathSequence = false;
        currentHealth = maxHealth;
        timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(currentHealth);
        OnRevive?.Invoke();

        Debug.Log($"{gameObject.name} ha sido revivido");
    }

    /// Revive la unidad con un porcentaje espec�fico de vida
    public void Revive(float healthPercentage)
    {
        // Cancelar cualquier secuencia de muerte en progreso
        CancelDeathSequence();

        IsDead = false;
        IsInDeathSequence = false;
        currentHealth = maxHealth * Mathf.Clamp01(healthPercentage);
        timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(currentHealth);
        OnRevive?.Invoke();

        Debug.Log($"{gameObject.name} ha sido revivido con {healthPercentage * 100}% de vida");
    }

    /// <summary>
    /// Cancela la secuencia de muerte en progreso (�til para revivir durante la animaci�n)
    /// </summary>
    public void CancelDeathSequence()
    {
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
            deathCoroutine = null;
        }
        IsInDeathSequence = false;
    }

    #endregion

    #region Configuraci�n de Delays

    /// <summary>
    /// Establece el delay de animaci�n de muerte en runtime
    /// </summary>
    public void SetDeathAnimationDelay(float delay)
    {
        deathAnimationDelay = Mathf.Max(0f, delay);
    }

    /// <summary>
    /// Establece el delay de destrucci�n en runtime
    /// </summary>
    public void SetDestroyDelay(float delay)
    {
        destroyDelay = Mathf.Max(0f, delay);
    }

    /// <summary>
    /// Establece el delay total (animaci�n + destrucci�n) distribuyendo autom�ticamente
    /// </summary>
    public void SetTotalDeathDelay(float totalDelay, float animationPortion = 0.7f)
    {
        totalDelay = Mathf.Max(0f, totalDelay);
        animationPortion = Mathf.Clamp01(animationPortion);

        deathAnimationDelay = totalDelay * animationPortion;
        destroyDelay = totalDelay * (1f - animationPortion);
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
    /// Verifica si est� a vida completa
    /// </summary>
    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    /// <summary>
    /// Verifica si la vida est� baja (menor al porcentaje especificado)
    /// </summary>
    public bool IsLowHealth(float percentage = 0.25f)
    {
        return HealthPercentage <= percentage;
    }

    /// <summary>
    /// Obtiene el tiempo total que tardar� la muerte (desde que muere hasta que se destruye)
    /// </summary>
    public float GetTotalDeathDuration()
    {
        return deathAnimationDelay + destroyDelay;
    }

    #endregion

    private void OnDisable()
    {
        // Limpiar coroutine si el objeto se desactiva
        CancelDeathSequence();
    }
}