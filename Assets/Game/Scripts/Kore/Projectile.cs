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

    [Header("Tags")]
    [SerializeField] private string tagJugador = "Player";
    [SerializeField] private string tagAliado = "Ally";
    [SerializeField] private string tagEscudo = "Shield";

    private Vector3 direction;
    private GameObject owner;
    private Rigidbody rb;
    private bool hasHit = false;
    private bool esProyectilAliado = false;

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

        // Determinar si es proyectil aliado (del jugador o de un aliado)
        if (owner != null)
        {
            esProyectilAliado = owner.CompareTag(tagJugador) || owner.CompareTag(tagAliado);
        }

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            // Si no hay Rigidbody, mover manualmente
            transform.position += direction * speed * Time.fixedDeltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Ignorar al dueño del proyectil
        if (owner != null && (other.transform == owner.transform || other.transform.IsChildOf(owner.transform)))
            return;

        // Si es proyectil aliado, ignorar el escudo
        if (esProyectilAliado && other.CompareTag(tagEscudo))
            return;

        // Verificar si está en las capas que puede golpear
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
            return;

        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        if (hasHit) return;

        // Ignorar al dueño del proyectil
        if (owner != null && (collision.transform == owner.transform || collision.transform.IsChildOf(owner.transform)))
            return;

        // Si es proyectil aliado, ignorar el escudo
        if (esProyectilAliado && collision.gameObject.CompareTag(tagEscudo))
            return;

        // Verificar si está en las capas que puede golpear
        if (((1 << collision.gameObject.layer) & hitLayers) == 0)
            return;

        // Efectos visuales
        if (!collision.gameObject.CompareTag(tagEscudo))
        {
            if (hitEffectN != null)
            {
                GameObject effect = Instantiate(hitEffectN, contact.point, Quaternion.identity);
                effect.transform.rotation = Quaternion.LookRotation(contact.normal);
                Destroy(effect, 1);
            }
        }
        else
        {
            if (hitEffectS != null)
            {
                GameObject effect = Instantiate(hitEffectS, contact.point, Quaternion.identity);
                effect.transform.rotation = Quaternion.LookRotation(contact.normal);
                Destroy(effect, 1);
            }
        }

        HandleHit(collision.collider);
    }

    private void HandleHit(Collider hitCollider)
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
}