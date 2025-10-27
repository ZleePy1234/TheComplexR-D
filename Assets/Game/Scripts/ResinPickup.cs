using UnityEngine;

public class ResinPickup : MonoBehaviour
{
    public int resinAmount;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.playerResin += resinAmount;
                Destroy(gameObject);
            }
        }
    }
}
