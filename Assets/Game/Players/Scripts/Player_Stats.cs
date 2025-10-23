using UnityEngine;
using com.cyborgAssets.inspectorButtonPro;

public class PlayerStats : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int plating;
    public int platingMax = 3;

    public float speedMultiplier = 1.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        currentHealth = maxHealth;
        plating = 0;

    }

    public void DamagePlayer(int damage)
    {
        if (plating > 0)
        {
            plating--;
            return;
        }
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        Debug.Log("Player Died");
    }
    public void HealPlayer(int healAmount)
    {
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
}
