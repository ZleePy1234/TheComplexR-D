using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Efectos")]
    [SerializeField] private LayerMask hitLayers = -1;
    [SerializeField] private GameObject hitEffectN;
    [SerializeField] private GameObject hitEffectS;

    [Header("Tags de Identificación")]
    [SerializeField] private string tagJugador = "Player";
    [SerializeField] private string tagAliado = "Ally";
    [SerializeField] private string tagEnemigo = "Enemy";
    [SerializeField] private string tagEscudo = "Shield";

    [Header("Tags a Ignorar - Proyectiles Aliados")]
    [Tooltip("Tags que los proyectiles del Player y Aliados atravesarán sin colisionar")]
    [SerializeField]
    private List<string> tagsIgnoradosPorAliados = new List<string>
    {
        "Player",
        "Ally",
        "Shield"
    };

    [Header("Tags a Ignorar - Proyectiles Enemigos")]
    [Tooltip("Tags que los proyectiles de Enemigos atravesarán sin colisionar")]
    [SerializeField]
    private List<string> tagsIgnoradosPorEnemigos = new List<string>
    {
        "Enemy",
        "Boss"
    };

    private Vector3 direction;
    private GameObject owner;
    private Rigidbody rb;
    private bool hasHit = false;
    private bool esProyectilAliado = false;
    private List<string> tagsAIgnorar = new List<string>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 dir, float spd, float dmg, GameObject ownerObject)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        owner = ownerObject;

        // Determinar si es proyectil aliado y configurar tags a ignorar
        if (owner != null)
        {
            esProyectilAliado = owner.CompareTag(tagJugador) || owner.CompareTag(tagAliado);

            // Asignar la lista de tags a ignorar según el tipo de disparador
            if (esProyectilAliado)
            {
                tagsAIgnorar = new List<string>(tagsIgnoradosPorAliados);
            }
            else
            {
                tagsAIgnorar = new List<string>(tagsIgnoradosPorEnemigos);
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// Inicialización extendida que permite especificar tags adicionales a ignorar
    /// </summary>
    public void Initialize(Vector3 dir, float spd, float dmg, GameObject ownerObject, List<string> tagsAdicionalesAIgnorar)
    {
        Initialize(dir, spd, dmg, ownerObject);

        // Agregar tags adicionales a la lista
        if (tagsAdicionalesAIgnorar != null)
        {
            foreach (string tag in tagsAdicionalesAIgnorar)
            {
                if (!tagsAIgnorar.Contains(tag))
                {
                    tagsAIgnorar.Add(tag);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            // Si no hay Rigidbody, mover manualmente
            transform.position += direction * speed * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Verifica si el proyectil debe ignorar la colisión con este objeto
    /// </summary>
    private bool DebeIgnorarColision(GameObject objeto)
    {
        // Siempre ignorar al dueño del proyectil
        if (owner != null && (objeto.transform == owner.transform || objeto.transform.IsChildOf(owner.transform)))
            return true;

        // Verificar si el tag del objeto está en la lista de tags a ignorar
        foreach (string tag in tagsAIgnorar)
        {
            if (objeto.CompareTag(tag))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifica si el objeto está en las capas que el proyectil puede golpear
    /// </summary>
    private bool EstaEnCapaValida(GameObject objeto)
    {
        return ((1 << objeto.layer) & hitLayers) != 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Verificar si debe ignorar esta colisión
        if (DebeIgnorarColision(other.gameObject))
            return;

        // Verificar si está en las capas que puede golpear
        if (!EstaEnCapaValida(other.gameObject))
            return;

        HandleHit(other, other.transform.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // Verificar si debe ignorar esta colisión
        if (DebeIgnorarColision(collision.gameObject))
            return;

        // Verificar si está en las capas que puede golpear
        if (!EstaEnCapaValida(collision.gameObject))
            return;

        ContactPoint contact = collision.contacts[0];

        // Efectos visuales
        SpawnHitEffect(collision.gameObject, contact.point, contact.normal);

        HandleHit(collision.collider, contact.point);
    }

    private void SpawnHitEffect(GameObject objetoGolpeado, Vector3 punto, Vector3 normal)
    {
        GameObject effectToSpawn = null;

        // Determinar qué efecto usar
        if (objetoGolpeado.CompareTag(tagEscudo))
        {
            effectToSpawn = hitEffectS;
        }
        else
        {
            effectToSpawn = hitEffectN;
        }

        // Instanciar el efecto
        if (effectToSpawn != null)
        {
            GameObject effect = Instantiate(effectToSpawn, punto, Quaternion.identity);
            effect.transform.rotation = Quaternion.LookRotation(normal);
            Destroy(effect, 1f);
        }
    }

    private void HandleHit(Collider hitCollider, Vector3 hitPoint)
    {
        hasHit = true;

        // Verificar si golpeó al jugador (usa PlayerStats)
        if (hitCollider.CompareTag(tagJugador))
        {
            PlayerStats playerStats = hitCollider.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.DamagePlayer(Mathf.RoundToInt(damage));
            }
        }
        else
        {
            // Para otros objetos, usar HealthSystem
            HealthSystem health = hitCollider.GetComponent<HealthSystem>();
            if (health != null && !health.IsDead)
            {
                health.TakeDamage(damage);
            }
        }

        // Destruir proyectil
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    #region Métodos Públicos de Utilidad

    /// <summary>
    /// Agrega un tag a la lista de tags a ignorar en runtime
    /// </summary>
    public void AgregarTagAIgnorar(string tag)
    {
        if (!string.IsNullOrEmpty(tag) && !tagsAIgnorar.Contains(tag))
        {
            tagsAIgnorar.Add(tag);
        }
    }

    /// <summary>
    /// Remueve un tag de la lista de tags a ignorar en runtime
    /// </summary>
    public void RemoverTagAIgnorar(string tag)
    {
        tagsAIgnorar.Remove(tag);
    }

    /// <summary>
    /// Verifica si el proyectil está ignorando un tag específico
    /// </summary>
    public bool EstaIgnorandoTag(string tag)
    {
        return tagsAIgnorar.Contains(tag);
    }

    /// <summary>
    /// Obtiene el dueño del proyectil
    /// </summary>
    public GameObject GetOwner()
    {
        return owner;
    }

    /// <summary>
    /// Verifica si es un proyectil aliado
    /// </summary>
    public bool EsProyectilAliado()
    {
        return esProyectilAliado;
    }

    #endregion
}