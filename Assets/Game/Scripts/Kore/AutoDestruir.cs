using UnityEngine;

public class AutoDestruir : MonoBehaviour
{
    public float tiempoDeVida = 2f;

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }
}
