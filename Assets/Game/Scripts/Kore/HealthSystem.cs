using UnityEngine;
using UnityEngine.Events;

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

    [Header("Eventos")]
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;

    private void Awake()
    {
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

        Debug.Log($"{gameObject.name} ha sido revivido");
    }

    /// Revive la unidad con un porcentaje específico de vida
    public void Revive(float healthPercentage)
    {
        IsDead = false;
        currentHealth = maxHealth * Mathf.Clamp01(healthPercentage);
        timeSinceLastDamage = 0f;
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"{gameObject.name} ha sido revivido con {healthPercentage * 100}% de vida");
    }
}