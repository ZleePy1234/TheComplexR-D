using System.Collections.Generic;
using UnityEngine;

public class AIAttackSystem : MonoBehaviour
{
    [System.Serializable]
    public class TagPriority
    {
        public string tag;
        [Tooltip("Mayor número = Mayor prioridad (solo para enemigos)")]
        public int priority;
    }

    public enum AIType
    {
        Enemy,  // Usa sistema de prioridades
        Ally    // Ataca al primer enemigo detectado
    }

    public enum AttackType
    {
        SingleTarget,    // Ataque a un solo objetivo
        AreaOfEffect,    // Daño en área alrededor del objetivo
        Cone,            // Daño en cono desde el atacante
        Laser,           // Daño en línea recta (rayo láser)
        Shotgun          // Daño tipo escopeta (múltiples rayos en cono)
    }

    [Header("Tipo de IA")]
    [SerializeField] private AIType aiType = AIType.Enemy;

    [Header("Tipo de Ataque")]
    [SerializeField] private AttackType attackType = AttackType.SingleTarget;

    [Header("Configuración de Detección")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private Vector3 detectionAreaSize = new Vector3(15f, 10f, 15f);
    [SerializeField] private Vector3 detectionAreaOffset = Vector3.zero;
    [SerializeField] private LayerMask detectionLayer = -1;
    [SerializeField] private float detectionInterval = 0.2f;
    [SerializeField] private bool useBoxDetection = false;

    [Header("Tags y Prioridades (Solo Enemigos)")]
    [SerializeField]
    private List<TagPriority> tagPriorities = new List<TagPriority>
    {
        new TagPriority { tag = "Player", priority = 10 },
        new TagPriority { tag = "Ally", priority = 8 },
        new TagPriority { tag = "Tank", priority = 5 }
    };

    [Header("Tags Objetivos (Solo Aliados)")]
    [SerializeField] private List<string> enemyTags = new List<string> { "Enemy", "Boss" };

    [Header("Configuración de Ataque Base")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Configuración: Area of Effect")]
    [SerializeField] private float aoeRadius = 5f;
    [SerializeField] private bool aoeDamageFalloff = true;
    [SerializeField] private AnimationCurve aoeFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0.3f);

    [Header("Configuración: Cono")]
    [SerializeField] private float coneAngle = 45f;
    [SerializeField] private float coneRange = 10f;

    [Header("Configuración: Laser")]
    [SerializeField] private float laserWidth = 0.5f;
    [SerializeField] private float laserRange = 15f;
    [SerializeField] private bool laserPierceTargets = true;
    [SerializeField] private int laserMaxTargets = 5;

    [Header("Configuración: Escopeta")]
    [SerializeField] private int shotgunPellets = 8;
    [SerializeField] private float shotgunSpread = 30f;
    [SerializeField] private float shotgunRange = 8f;
    [SerializeField] private float shotgunDamagePerPellet = 5f;
    [SerializeField] private bool shotgunDetectAllInCone = true;
    [SerializeField] private float shotgunPelletRadius = 0.3f;

    [Header("Proyectil (Opcional)")]
    [SerializeField] private bool useProjectile = false;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 20f;

    [Header("Efectos Visuales (Opcional)")]
    [SerializeField] private GameObject muzzleFlashEffect;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject aoeEffect;
    [SerializeField] private GameObject laserEffect;
    [SerializeField] private float effectDuration = 1f;

    [Header("Animacion")]
    [SerializeField] private Animator animator;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool showDebugLogs = true;

    [Header("Configuración de Targeting")]
    [SerializeField] private bool stickyTargeting = true; // Mantener objetivo hasta que muera
    [SerializeField] private bool onlyChangeForHigherPriority = true;

    // Variables privadas
    private Transform currentTarget;
    private int currentPriority = -1;
    private float detectionTimer;
    private float attackTimer;
    private Dictionary<string, int> priorityDict;
    private List<Collider> detectedObjects = new List<Collider>();

    // Propiedades públicas
    public Transform CurrentTarget => currentTarget;
    public int CurrentPriority => currentPriority;
    public bool HasTarget => currentTarget != null;
    public bool CanAttack => attackTimer <= 0f;

    private void Start()
    {
        // Inicializar diccionario de prioridades solo para enemigos
        if (aiType == AIType.Enemy)
        {
            priorityDict = new Dictionary<string, int>();
            foreach (var tp in tagPriorities)
            {
                if (!string.IsNullOrEmpty(tp.tag))
                {
                    priorityDict[tp.tag] = tp.priority;
                }
            }
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Validar configuración de proyectiles
        if (useProjectile)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"{gameObject.name}: Proyectil activado pero no hay prefab asignado");
                useProjectile = false;
            }
            if (firePoint == null)
            {
                firePoint = transform;
                Debug.LogWarning($"{gameObject.name}: No hay FirePoint asignado, usando transform principal");
            }
        }

        attackTimer = 0f;
    }

    private void Update()
    {
        // Actualizar timers
        detectionTimer += Time.deltaTime;
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        // Detectar objetivos
        if (detectionTimer >= detectionInterval)
        {
            detectionTimer = 0f;
            DetectTargets();
        }

        // Atacar si hay objetivo válido
        if (currentTarget != null && CanAttack)
        {
            TryAttack();
        }
    }

    /// <summary>
    /// Detecta objetivos según el tipo de IA
    /// </summary>
    private void DetectTargets()
    {
        detectedObjects.Clear();
        Collider[] colliders;

        // Detectar objetos según el método configurado
        if (useBoxDetection)
        {
            Vector3 center = transform.position + transform.TransformDirection(detectionAreaOffset);
            colliders = Physics.OverlapBox(
                center,
                detectionAreaSize / 2f,
                transform.rotation,
                detectionLayer
            );
        }
        else
        {
            colliders = Physics.OverlapSphere(transform.position, detectionRange, detectionLayer);
        }

        if (aiType == AIType.Enemy)
        {
            DetectTargetsAsEnemy(colliders);
        }
        else
        {
            DetectTargetsAsAlly(colliders);
        }
    }

    /// <summary>
    /// Detección para IA enemiga (con sistema de prioridades)
    /// </summary>
    private void DetectTargetsAsEnemy(Collider[] colliders)
    {
        if (stickyTargeting && currentTarget != null)
        {
            HealthSystem currentHealth = currentTarget.GetComponent<HealthSystem>();
            bool isAlive = currentHealth != null && !currentHealth.IsDead;

            if (isAlive)
            {
                // Verificar si sigue en rango
                bool stillInRange = System.Array.Exists(colliders,
                    col => col.transform == currentTarget);

                if (stillInRange)
                {
                    // Solo cambiar si encontramos mayor prioridad
                    if (onlyChangeForHigherPriority)
                    {
                        Transform bestTarget = null;
                        int bestPriority = currentPriority;

                        foreach (var col in colliders)
                        {
                            if (col.transform == transform || col.transform.IsChildOf(transform))
                                continue;

                            if (priorityDict.TryGetValue(col.tag, out int priority))
                            {
                                if (priority > bestPriority) // Mayor que el actual
                                {
                                    if (!requireLineOfSight || HasLineOfSight(col.transform))
                                    {
                                        bestPriority = priority;
                                        bestTarget = col.transform;
                                    }
                                }
                            }
                        }

                        if (bestTarget != null)
                        {
                            UpdateTarget(bestTarget, bestPriority);
                        }
                    }

                    return; // Mantener objetivo actual
                }
            }
        }

        // Buscar nuevo objetivo (código original)
        Transform newBestTarget = null;
        int newBestPriority = -1;

        foreach (var col in colliders)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            detectedObjects.Add(col);

            if (priorityDict.TryGetValue(col.tag, out int priority))
            {
                if (requireLineOfSight && !HasLineOfSight(col.transform))
                    continue;

                if (priority > newBestPriority)
                {
                    newBestPriority = priority;
                    newBestTarget = col.transform;
                }
            }
        }

        UpdateTarget(newBestTarget, newBestPriority);
    }

    /// <summary>
    /// Detección para IA aliada (primer enemigo detectado)
    /// </summary>
    private void DetectTargetsAsAlly(Collider[] colliders)
    {
        Transform newTarget = null;

        foreach (var col in colliders)
        {
            // Ignorar a sí mismo
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            detectedObjects.Add(col);

            // Verificar si tiene uno de los tags enemigos
            if (enemyTags.Contains(col.tag))
            {
                // Verificar línea de visión si está activado
                if (requireLineOfSight && !HasLineOfSight(col.transform))
                    continue;

                newTarget = col.transform;
                break; // Tomar el primer enemigo encontrado
            }
        }

        UpdateTarget(newTarget, newTarget != null ? 1 : -1);
    }

    /// <summary>
    /// Actualiza el objetivo actual
    /// </summary>
    private void UpdateTarget(Transform newTarget, int priority)
    {
        if (newTarget != currentTarget)
        {
            Transform previousTarget = currentTarget;
            currentTarget = newTarget;
            currentPriority = priority;

            if (showDebugLogs)
            {
                if (newTarget != null)
                {
                    Debug.Log($"{gameObject.name} [{aiType}]: Nuevo objetivo - {newTarget.name} (Tag: {newTarget.tag}, Prioridad: {currentPriority})");
                }
                else if (previousTarget != null)
                {
                    Debug.Log($"{gameObject.name} [{aiType}]: Objetivo perdido - {previousTarget.name}");
                }
            }
        }
    }

    /// <summary>
    /// Verifica si hay línea de visión con el objetivo
    /// </summary>
    private bool HasLineOfSight(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, distance, obstacleLayer))
        {
            // Si el raycast golpea algo antes de llegar al objetivo, no hay línea de visión
            return hit.transform == target;
        }

        return true;
    }

    /// <summary>
    /// Intenta atacar al objetivo actual
    /// </summary>
    private void TryAttack()
    {
        if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        // Verificar si está en rango de ataque
        if (distanceToTarget <= attackRange)
        {
            // Verificar línea de visión una última vez antes de atacar
            if (requireLineOfSight && !HasLineOfSight(currentTarget))
                return;

            PerformAttack();
        }
    }

    /// <summary>
    /// Ejecuta el ataque según el tipo configurado
    /// </summary>
    private void PerformAttack()
    {
        attackTimer = 1f / attackRate;

        // Efecto de disparo
        if (muzzleFlashEffect != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashEffect, firePoint.position, firePoint.rotation);
            Destroy(flash, effectDuration);
        }

        if (useProjectile)
        {
            SpawnProjectile();
        }
        else
        {
            // Ejecutar ataque instantáneo según el tipo
            switch (attackType)
            {
                case AttackType.SingleTarget:
                    SingleTargetAttack();
                    break;
                case AttackType.AreaOfEffect:
                    AreaOfEffectAttack();
                    break;
                case AttackType.Cone:
                    ConeAttack();
                    break;
                case AttackType.Laser:
                    LaserAttack();
                    break;
                case AttackType.Shotgun:
                    ShotgunAttack();
                    break;
            }
        }

        animator.SetTrigger("Shoot");

    }

    /// <summary>
    /// Ataque a un solo objetivo
    /// </summary>
    private void SingleTargetAttack()
    {
        if (currentTarget == null) return;

        ApplyDamage(currentTarget, attackDamage);

        if (hitEffect != null)
        {
            GameObject hit = Instantiate(hitEffect, currentTarget.position, Quaternion.identity);
            Destroy(hit, effectDuration);
        }



        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} atacó a {currentTarget.name} con {attackDamage} de daño [SingleTarget]");
        }
    }

    /// <summary>
    /// Ataque en área alrededor del objetivo
    /// </summary>
    private void AreaOfEffectAttack()
    {
        if (currentTarget == null) return;

        Vector3 explosionCenter = currentTarget.position;
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, aoeRadius, detectionLayer);

        int targetsHit = 0;
        foreach (Collider col in hitColliders)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            float distance = Vector3.Distance(explosionCenter, col.transform.position);
            float damageMultiplier = 1f;

            if (aoeDamageFalloff)
            {
                float normalizedDistance = distance / aoeRadius;
                damageMultiplier = aoeFalloffCurve.Evaluate(normalizedDistance);
            }

            float finalDamage = attackDamage * damageMultiplier;
            if (ApplyDamage(col.transform, finalDamage))
            {
                targetsHit++;
            }
        }

        // Efecto visual de explosión
        if (aoeEffect != null)
        {
            GameObject effect = Instantiate(aoeEffect, explosionCenter, Quaternion.identity);
            effect.transform.localScale = Vector3.one * aoeRadius * 2f;
            Destroy(effect, effectDuration);
        }

        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} ataque AoE - {targetsHit} objetivos alcanzados");
        }
    }

    /// <summary>
    /// Ataque en cono desde el atacante
    /// </summary>
    private void ConeAttack()
    {
        if (currentTarget == null) return;

        Vector3 attackDirection = (currentTarget.position - transform.position).normalized;
        Vector3 attackOrigin = firePoint != null ? firePoint.position : transform.position;

        Collider[] potentialTargets = Physics.OverlapSphere(attackOrigin, coneRange, detectionLayer);

        int targetsHit = 0;
        foreach (Collider col in potentialTargets)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            Vector3 directionToTarget = (col.transform.position - attackOrigin).normalized;
            float angle = Vector3.Angle(attackDirection, directionToTarget);

            if (angle <= coneAngle / 2f)
            {
                float distance = Vector3.Distance(attackOrigin, col.transform.position);
                if (distance <= coneRange)
                {
                    if (ApplyDamage(col.transform, attackDamage))
                    {
                        targetsHit++;

                        if (hitEffect != null)
                        {
                            GameObject hit = Instantiate(hitEffect, col.transform.position, Quaternion.identity);
                            Destroy(hit, effectDuration);
                        }
                    }
                }
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} ataque en Cono - {targetsHit} objetivos alcanzados");
        }
    }

    /// <summary>
    /// Ataque tipo láser en línea recta
    /// </summary>
    private void LaserAttack()
    {
        if (currentTarget == null) return;

        Vector3 attackOrigin = firePoint != null ? firePoint.position : transform.position;
        Vector3 attackDirection = (currentTarget.position - attackOrigin).normalized;

        List<Transform> hitTargets = new List<Transform>();
        RaycastHit[] hits = Physics.RaycastAll(attackOrigin, attackDirection, laserRange, detectionLayer);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            if (hitTargets.Contains(hit.transform))
                continue;

            if (!laserPierceTargets && hitTargets.Count > 0)
                break;

            if (hitTargets.Count >= laserMaxTargets)
                break;

            if (ApplyDamage(hit.transform, attackDamage))
            {
                hitTargets.Add(hit.transform);

                if (hitEffect != null)
                {
                    GameObject hitFx = Instantiate(hitEffect, hit.point, Quaternion.identity);
                    Destroy(hitFx, effectDuration);
                }
            }
        }

        // Efecto visual del láser
        if (laserEffect != null)
        {
            GameObject laser = Instantiate(laserEffect, attackOrigin, Quaternion.LookRotation(attackDirection));
            LineRenderer lr = laser.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, attackOrigin);
                lr.SetPosition(1, attackOrigin + attackDirection * laserRange);
            }
            Destroy(laser, effectDuration);
        }

        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} ataque Láser - {hitTargets.Count} objetivos alcanzados");
        }
    }

    /// <summary>
    /// Ataque tipo escopeta (múltiples rayos en cono)
    /// </summary>
    private void ShotgunAttack()
    {
        if (currentTarget == null) return;

        Vector3 attackOrigin = firePoint != null ? firePoint.position : transform.position;
        Vector3 baseDirection = (currentTarget.position - attackOrigin).normalized;

        Dictionary<Transform, int> targetHits = new Dictionary<Transform, int>();
        HashSet<Transform> allTargetsInCone = new HashSet<Transform>();

        if (shotgunDetectAllInCone)
        {
            // Primero, detectar TODOS los objetivos en el cono
            Collider[] potentialTargets = Physics.OverlapSphere(attackOrigin, shotgunRange, detectionLayer);

            foreach (Collider col in potentialTargets)
            {
                if (col.transform == transform || col.transform.IsChildOf(transform))
                    continue;

                Vector3 directionToTarget = (col.transform.position - attackOrigin).normalized;
                float angle = Vector3.Angle(baseDirection, directionToTarget);

                // Si está dentro del cono de dispersión
                if (angle <= shotgunSpread / 2f)
                {
                    float distance = Vector3.Distance(attackOrigin, col.transform.position);
                    if (distance <= shotgunRange)
                    {
                        // Verificar obstáculos
                        if (Physics.Raycast(attackOrigin, directionToTarget, out RaycastHit obstacleCheck, distance, obstacleLayer))
                        {
                            if (obstacleCheck.transform != col.transform)
                                continue; // Bloqueado
                        }

                        allTargetsInCone.Add(col.transform);
                    }
                }
            }
        }

        // Disparar perdigones individuales
        for (int i = 0; i < shotgunPellets; i++)
        {
            // Calcular dirección con dispersión
            float randomAngleH = Random.Range(-shotgunSpread / 2f, shotgunSpread / 2f);
            float randomAngleV = Random.Range(-shotgunSpread / 2f, shotgunSpread / 2f);

            Quaternion spread = Quaternion.Euler(randomAngleV, randomAngleH, 0);
            Vector3 pelletDirection = spread * baseDirection;

            // SphereCast para cada perdigón (simula grosor del perdigón)
            RaycastHit[] hits = Physics.SphereCastAll(attackOrigin, shotgunPelletRadius, pelletDirection, shotgunRange, detectionLayer);

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                // Verificar si hay obstáculo antes del objetivo
                if (Physics.Raycast(attackOrigin, pelletDirection, out RaycastHit obstacleCheck, hit.distance, obstacleLayer))
                {
                    if (obstacleCheck.transform != hit.transform)
                        continue; // Bloqueado por obstáculo
                }

                // Contar impactos por objetivo
                if (!targetHits.ContainsKey(hit.transform))
                {
                    targetHits[hit.transform] = 0;
                }
                targetHits[hit.transform]++;

                // Si detectamos all in cone, asegurar que esté en la lista
                if (shotgunDetectAllInCone)
                {
                    allTargetsInCone.Add(hit.transform);
                }
            }
        }

        // Si está activado detectar todos en cono, asegurar que reciban al menos 1 impacto
        if (shotgunDetectAllInCone)
        {
            foreach (Transform target in allTargetsInCone)
            {
                if (!targetHits.ContainsKey(target))
                {
                    // Objetivo en el cono pero no golpeado por ningún perdigón
                    // Darle al menos 1 impacto para que no sea ignorado
                    targetHits[target] = 1;
                }
            }
        }

        // Aplicar daño a cada objetivo según los impactos
        foreach (var kvp in targetHits)
        {
            float totalDamage = shotgunDamagePerPellet * kvp.Value;
            ApplyDamage(kvp.Key, totalDamage);

            if (hitEffect != null)
            {
                GameObject hit = Instantiate(hitEffect, kvp.Key.position, Quaternion.identity);
                Destroy(hit, effectDuration);
            }
        }

        if (showDebugLogs)
        {
            int totalPelletsHit = 0;
            foreach (var hits in targetHits.Values)
                totalPelletsHit += hits;

            Debug.Log($"{gameObject.name} ataque Escopeta - {targetHits.Count} objetivos alcanzados, {totalPelletsHit}/{shotgunPellets} perdigones impactaron");
        }
    }

    /// <summary>
    /// Aplica daño a un objetivo
    /// </summary>
    private bool ApplyDamage(Transform target, float damage)
    {
        HealthSystem health = target.GetComponent<HealthSystem>();
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Crea un proyectil
    /// </summary>
    private void SpawnProjectile()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (currentTarget.position - spawnPos).normalized;

        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.Initialize(direction, projectileSpeed, attackDamage, gameObject);
        }
        else
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * projectileSpeed;
            }
        }
    }

    /// <summary>
    /// Fuerza la detección inmediata
    /// </summary>
    public void ForceDetection()
    {
        DetectTargets();
    }

    /// <summary>
    /// Limpia el objetivo actual
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
        currentPriority = -1;
    }

    /// <summary>
    /// Obtiene la distancia al objetivo actual
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector3.Distance(transform.position, currentTarget.position);
    }

    /// <summary>
    /// Verifica si está en rango de ataque
    /// </summary>
    public bool IsInAttackRange()
    {
        return currentTarget != null && GetDistanceToTarget() <= attackRange;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Dibujar rango de detección
        Gizmos.color = currentTarget != null ? new Color(1f, 0f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.3f);

        if (useBoxDetection)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(detectionAreaOffset, detectionAreaSize);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }

        // Dibujar rango de ataque base
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Dibujar área específica según tipo de ataque
        if (currentTarget != null && Application.isPlaying)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 attackOrigin = firePoint != null ? firePoint.position : transform.position;
            Vector3 directionToTarget = (currentTarget.position - attackOrigin).normalized;

            switch (attackType)
            {
                case AttackType.SingleTarget:
                    // Línea al objetivo
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(attackOrigin, currentTarget.position);
                    Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
                    break;

                case AttackType.AreaOfEffect:
                    // Esfera alrededor del objetivo
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                    Gizmos.DrawWireSphere(currentTarget.position, aoeRadius);
                    break;

                case AttackType.Cone:
                    // Cono de ataque
                    Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                    DrawConeGizmo(attackOrigin, directionToTarget, coneAngle, coneRange);
                    break;

                case AttackType.Laser:
                    // Línea recta del láser
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(attackOrigin, attackOrigin + directionToTarget * laserRange);
                    Gizmos.DrawWireSphere(attackOrigin + directionToTarget * laserRange, laserWidth);
                    break;

                case AttackType.Shotgun:
                    // Cono de dispersión de escopeta
                    Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
                    DrawConeGizmo(attackOrigin, directionToTarget, shotgunSpread, shotgunRange);
                    break;
            }
        }

        // Dibujar punto de disparo
        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }

    private void DrawConeGizmo(Vector3 origin, Vector3 direction, float angle, float range)
    {
        int segments = 16;
        float halfAngle = angle / 2f;

        Vector3 forward = direction * range;
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 up = Vector3.Cross(direction, right).normalized;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = (i / (float)segments) * 360f;
            Vector3 offset = (Mathf.Cos(currentAngle * Mathf.Deg2Rad) * right +
                             Mathf.Sin(currentAngle * Mathf.Deg2Rad) * up) *
                             Mathf.Tan(halfAngle * Mathf.Deg2Rad) * range;

            Vector3 point = origin + forward + offset;
            Gizmos.DrawLine(origin, point);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);

        if (useBoxDetection)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(detectionAreaOffset, detectionAreaSize);
        }
        else
        {
            Gizmos.DrawSphere(transform.position, detectionRange);
        }
    }
}
