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

    [Header("Configuración de Apuntado")]
    [SerializeField] private float aimHeightOffset = 1.2f;
    [SerializeField] private bool useTargetAimPoint = true;
    [SerializeField] private string aimPointName = "AimPoint";

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
    [SerializeField] private bool stickyTargeting = true;
    [SerializeField] private bool onlyChangeForHigherPriority = true;

    // Variables privadas
    private Transform currentTarget;
    private int currentPriority = -1;
    private float detectionTimer;
    private float attackTimer;
    private Dictionary<string, int> priorityDict;
    private List<Collider> detectedObjects = new List<Collider>();

    // Cache del punto de apuntado
    private Transform cachedAimPoint;
    private Transform lastTargetChecked;

    // Propiedades públicas
    public Transform CurrentTarget => currentTarget;
    public int CurrentPriority => currentPriority;
    public bool HasTarget => currentTarget != null;
    public bool CanAttack => attackTimer <= 0f;

    private void Start()
    {
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
        detectionTimer += Time.deltaTime;
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        if (detectionTimer >= detectionInterval)
        {
            detectionTimer = 0f;
            DetectTargets();
        }

        if (currentTarget != null && CanAttack)
        {
            TryAttack();
        }
    }

    #region Sistema de Apuntado

    /// <summary>
    /// Obtiene la posición de apuntado del objetivo actual
    /// </summary>
    private Vector3 GetTargetAimPosition()
    {
        if (currentTarget == null) return Vector3.zero;

        // Buscar punto de apuntado específico en el objetivo
        if (useTargetAimPoint)
        {
            // Cache del AimPoint para evitar búsquedas repetidas
            if (lastTargetChecked != currentTarget)
            {
                lastTargetChecked = currentTarget;
                cachedAimPoint = currentTarget.Find(aimPointName);
            }

            if (cachedAimPoint != null)
            {
                return cachedAimPoint.position;
            }
        }

        // Fallback: usar offset de altura
        return currentTarget.position + Vector3.up * aimHeightOffset;
    }

    /// <summary>
    /// Obtiene la posición de apuntado de un transform específico
    /// </summary>
    private Vector3 GetAimPositionFor(Transform target)
    {
        if (target == null) return Vector3.zero;

        if (useTargetAimPoint)
        {
            Transform aimPoint = target.Find(aimPointName);
            if (aimPoint != null)
            {
                return aimPoint.position;
            }
        }

        return target.position + Vector3.up * aimHeightOffset;
    }

    #endregion

    #region Detección

    private void DetectTargets()
    {
        detectedObjects.Clear();
        Collider[] colliders;

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

    private void DetectTargetsAsEnemy(Collider[] colliders)
    {
        if (stickyTargeting && currentTarget != null)
        {
            HealthSystem currentHealth = currentTarget.GetComponent<HealthSystem>();
            bool isAlive = currentHealth != null && !currentHealth.IsDead;

            if (isAlive)
            {
                bool stillInRange = System.Array.Exists(colliders,
                    col => col.transform == currentTarget);

                if (stillInRange)
                {
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
                                if (priority > bestPriority)
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

                    return;
                }
            }
        }

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

    private void DetectTargetsAsAlly(Collider[] colliders)
    {
        Transform newTarget = null;

        foreach (var col in colliders)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            detectedObjects.Add(col);

            if (enemyTags.Contains(col.tag))
            {
                if (requireLineOfSight && !HasLineOfSight(col.transform))
                    continue;

                newTarget = col.transform;
                break;
            }
        }

        UpdateTarget(newTarget, newTarget != null ? 1 : -1);
    }

    private void UpdateTarget(Transform newTarget, int priority)
    {
        if (newTarget != currentTarget)
        {
            Transform previousTarget = currentTarget;
            currentTarget = newTarget;
            currentPriority = priority;

            // Limpiar cache de AimPoint
            cachedAimPoint = null;
            lastTargetChecked = null;

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

    private bool HasLineOfSight(Transform target)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * aimHeightOffset;
        Vector3 targetPos = GetAimPositionFor(target);
        Vector3 direction = targetPos - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, obstacleLayer))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return true;
    }

    #endregion

    #region Ataques

    private void TryAttack()
    {
        if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToTarget <= attackRange)
        {
            if (requireLineOfSight && !HasLineOfSight(currentTarget))
                return;

            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        attackTimer = 1f / attackRate;

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

        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
    }

    private void SingleTargetAttack()
    {
        if (currentTarget == null) return;

        ApplyDamage(currentTarget, attackDamage);

        if (hitEffect != null)
        {
            Vector3 hitPos = GetTargetAimPosition();
            GameObject hit = Instantiate(hitEffect, hitPos, Quaternion.identity);
            Destroy(hit, effectDuration);
        }

        if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} atacó a {currentTarget.name} con {attackDamage} de daño [SingleTarget]");
        }
    }

    private void AreaOfEffectAttack()
    {
        if (currentTarget == null) return;

        Vector3 explosionCenter = GetTargetAimPosition();
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, aoeRadius, detectionLayer);

        int targetsHit = 0;
        foreach (Collider col in hitColliders)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            Vector3 targetPos = GetAimPositionFor(col.transform);
            float distance = Vector3.Distance(explosionCenter, targetPos);
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

    private void ConeAttack()
    {
        if (currentTarget == null) return;

        Vector3 attackOrigin = firePoint != null ? firePoint.position : transform.position + Vector3.up * aimHeightOffset;
        Vector3 targetAimPos = GetTargetAimPosition();
        Vector3 attackDirection = (targetAimPos - attackOrigin).normalized;

        Collider[] potentialTargets = Physics.OverlapSphere(attackOrigin, coneRange, detectionLayer);

        int targetsHit = 0;
        foreach (Collider col in potentialTargets)
        {
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            Vector3 targetPos = GetAimPositionFor(col.transform);
            Vector3 directionToTarget = (targetPos - attackOrigin).normalized;
            float angle = Vector3.Angle(attackDirection, directionToTarget);

            if (angle <= coneAngle / 2f)
            {
                float distance = Vector3.Distance(attackOrigin, targetPos);
                if (distance <= coneRange)
                {
                    if (ApplyDamage(col.transform, attackDamage))
                    {
                        targetsHit++;

                        if (hitEffect != null)
                        {
                            GameObject hit = Instantiate(hitEffect, targetPos, Quaternion.identity);
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

    private void LaserAttack()
    {
        if (currentTarget == null) return;

        Vector3 attackOrigin = firePoint != null ? firePoint.position : transform.position + Vector3.up * aimHeightOffset;
        Vector3 targetAimPos = GetTargetAimPosition();
        Vector3 attackDirection = (targetAimPos - attackOrigin).normalized;

        List<Transform> hitTargets = new List<Transform>();
        RaycastHit[] hits = Physics.RaycastAll(attackOrigin, attackDirection, laserRange, detectionLayer);

        // Ordenar por distancia
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

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

    private void ShotgunAttack()
    {
        if (currentTarget == null) return;

        Vector3 attackOrigin = firePoint != null ? firePoint.position : transform.position + Vector3.up * aimHeightOffset;
        Vector3 targetAimPos = GetTargetAimPosition();
        Vector3 baseDirection = (targetAimPos - attackOrigin).normalized;

        Dictionary<Transform, int> targetHits = new Dictionary<Transform, int>();
        HashSet<Transform> allTargetsInCone = new HashSet<Transform>();

        if (shotgunDetectAllInCone)
        {
            Collider[] potentialTargets = Physics.OverlapSphere(attackOrigin, shotgunRange, detectionLayer);

            foreach (Collider col in potentialTargets)
            {
                if (col.transform == transform || col.transform.IsChildOf(transform))
                    continue;

                Vector3 targetPos = GetAimPositionFor(col.transform);
                Vector3 directionToTarget = (targetPos - attackOrigin).normalized;
                float angle = Vector3.Angle(baseDirection, directionToTarget);

                if (angle <= shotgunSpread / 2f)
                {
                    float distance = Vector3.Distance(attackOrigin, targetPos);
                    if (distance <= shotgunRange)
                    {
                        if (Physics.Raycast(attackOrigin, directionToTarget, out RaycastHit obstacleCheck, distance, obstacleLayer))
                        {
                            if (obstacleCheck.transform != col.transform && !obstacleCheck.transform.IsChildOf(col.transform))
                                continue;
                        }

                        allTargetsInCone.Add(col.transform);
                    }
                }
            }
        }

        for (int i = 0; i < shotgunPellets; i++)
        {
            float randomAngleH = Random.Range(-shotgunSpread / 2f, shotgunSpread / 2f);
            float randomAngleV = Random.Range(-shotgunSpread / 2f, shotgunSpread / 2f);

            Quaternion spread = Quaternion.Euler(randomAngleV, randomAngleH, 0);
            Vector3 pelletDirection = spread * baseDirection;

            RaycastHit[] hits = Physics.SphereCastAll(attackOrigin, shotgunPelletRadius, pelletDirection, shotgunRange, detectionLayer);

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                if (Physics.Raycast(attackOrigin, pelletDirection, out RaycastHit obstacleCheck, hit.distance, obstacleLayer))
                {
                    if (obstacleCheck.transform != hit.transform && !obstacleCheck.transform.IsChildOf(hit.transform))
                        continue;
                }

                if (!targetHits.ContainsKey(hit.transform))
                {
                    targetHits[hit.transform] = 0;
                }
                targetHits[hit.transform]++;

                if (shotgunDetectAllInCone)
                {
                    allTargetsInCone.Add(hit.transform);
                }
            }
        }

        if (shotgunDetectAllInCone)
        {
            foreach (Transform target in allTargetsInCone)
            {
                if (!targetHits.ContainsKey(target))
                {
                    targetHits[target] = 1;
                }
            }
        }

        foreach (var kvp in targetHits)
        {
            float totalDamage = shotgunDamagePerPellet * kvp.Value;
            ApplyDamage(kvp.Key, totalDamage);

            if (hitEffect != null)
            {
                Vector3 hitPos = GetAimPositionFor(kvp.Key);
                GameObject hit = Instantiate(hitEffect, hitPos, Quaternion.identity);
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

    private void SpawnProjectile()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * aimHeightOffset;
        Vector3 targetAimPos = GetTargetAimPosition();
        Vector3 direction = (targetAimPos - spawnPos).normalized;

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

    #endregion

    #region Utilidades Públicas

    public void ForceDetection()
    {
        DetectTargets();
    }

    public void ClearTarget()
    {
        currentTarget = null;
        currentPriority = -1;
        cachedAimPoint = null;
        lastTargetChecked = null;
    }

    public float GetDistanceToTarget()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector3.Distance(transform.position, currentTarget.position);
    }

    public bool IsInAttackRange()
    {
        return currentTarget != null && GetDistanceToTarget() <= attackRange;
    }

    /// <summary>
    /// Obtiene la posición actual de apuntado (útil para otros sistemas)
    /// </summary>
    public Vector3 GetCurrentAimPosition()
    {
        return GetTargetAimPosition();
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

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

        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentTarget != null && Application.isPlaying)
        {
            Vector3 attackOrigin = firePoint != null ? firePoint.position : transform.position + Vector3.up * aimHeightOffset;
            Vector3 targetAimPos = GetTargetAimPosition();
            Vector3 directionToTarget = (targetAimPos - attackOrigin).normalized;

            // Dibujar punto de apuntado
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetAimPos, 0.3f);

            switch (attackType)
            {
                case AttackType.SingleTarget:
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(attackOrigin, targetAimPos);
                    break;

                case AttackType.AreaOfEffect:
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                    Gizmos.DrawWireSphere(targetAimPos, aoeRadius);
                    Gizmos.DrawLine(attackOrigin, targetAimPos);
                    break;

                case AttackType.Cone:
                    Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                    DrawConeGizmo(attackOrigin, directionToTarget, coneAngle, coneRange);
                    break;

                case AttackType.Laser:
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(attackOrigin, attackOrigin + directionToTarget * laserRange);
                    Gizmos.DrawWireSphere(attackOrigin + directionToTarget * laserRange, laserWidth);
                    break;

                case AttackType.Shotgun:
                    Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
                    DrawConeGizmo(attackOrigin, directionToTarget, shotgunSpread, shotgunRange);
                    break;
            }
        }

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

    #endregion
}