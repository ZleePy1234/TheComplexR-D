using UnityEngine;

public class DestroyBullet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Level"))
        {
            Destroy(gameObject);
        }
    }
}
