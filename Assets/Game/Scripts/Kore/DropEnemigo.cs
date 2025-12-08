using UnityEngine;

public class DropEnemigo : MonoBehaviour
{
    [Header("Configuración de Drop")]
    [SerializeField] private GameObject prefabDrop;
    [SerializeField] private Transform puntoSpawn;

    [Header("Probabilidad")]
    [Range(0f, 100f)]
    [SerializeField] private float probabilidadDrop = 100f;

    [Header("Offset de Spawn")]
    [SerializeField] private Vector3 offsetSpawn = Vector3.zero;
        
    public void IntentarDrop()
    {
        if (prefabDrop == null) return;

        // Verificar probabilidad
        float random = Random.Range(0f, 100f);
        if (random > probabilidadDrop) return;

        // Determinar posición de spawn
        Vector3 posicionSpawn;

        if (puntoSpawn != null)
        {
            posicionSpawn = puntoSpawn.position + offsetSpawn;
        }
        else
        {
            posicionSpawn = transform.position + offsetSpawn;
        }

        // Instanciar el drop
        Instantiate(prefabDrop, posicionSpawn, Quaternion.identity);
    }
}