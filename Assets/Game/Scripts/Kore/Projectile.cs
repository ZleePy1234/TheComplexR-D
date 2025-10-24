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

    private Vector3 direction;
    private GameObject owner;
    private Rigidbody rb;
    private bool hasHit = false;

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

        // Verificar si está en las capas que puede golpear
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
            return;

        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // Ignorar al dueño del proyectil
        if (owner != null && (collision.transform == owner.transform || collision.transform.IsChildOf(owner.transform)))
            return;

        // Verificar si está en las capas que puede golpear
        if (((1 << collision.gameObject.layer) & hitLayers) == 0)
            return;

        HandleHit(collision.collider);
    }

    private void HandleHit(Collider hitCollider)
    {
        hasHit = true;

        // Aplicar daño si el objeto tiene HealthSystem
        HealthSystem health = hitCollider.GetComponent<HealthSystem>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
        }

        // Destruir proyectil
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}