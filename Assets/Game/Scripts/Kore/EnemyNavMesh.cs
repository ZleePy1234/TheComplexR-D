using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sistema de IA para enemigos usando NavMesh.
/// Detecta y persigue objetivos basándose en prioridades de tags.
/// Mantiene distancia óptima del objetivo.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavMesh : MonoBehaviour
{
    [System.Serializable]
    public class TagPriority
    {
        public string tag;
        [Tooltip("Mayor número = Mayor prioridad")]
        public int priority;
    }

    [Header("Configuración de Detección")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private LayerMask detectionLayer = -1;
    [SerializeField] private float detectionInterval = 0.2f;

    [Header("Tags y Prioridades")]
    [SerializeField]
    private List<TagPriority> tagPriorities = new List<TagPriority>
    {
        new TagPriority { tag = "Player", priority = 10 },
        new TagPriority { tag = "Tank", priority = 5 },
        new TagPriority { tag = "Ally", priority = 1 }
    };

    [Header("Configuración de Movimiento")]
    [SerializeField] private float optimalDistance = 5f;
    [SerializeField] private float distanceTolerance = 1f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("NavMesh")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float angularSpeed = 120f;
    [SerializeField] private float acceleration = 8f;

    [Header("Comportamiento")]
    [SerializeField] private bool maintainDistance = true;
    [SerializeField] private bool stickyTargeting = true;

    [Header("Movimiento Orgánico")]
    [SerializeField] private bool enableIdleMovement = true;
    [SerializeField] private float idleMoveInterval = 2f;
    [SerializeField] private float idleMoveIntervalVariance = 1f;
    [Tooltip("Ángulo máximo de movimiento desde la posición actual (±grados)")]
    [SerializeField] private float idleArcAngle = 45f;
    [SerializeField] private float idleMinDistance = 1f;
    [SerializeField] private float idleMaxDistance = 3f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    // Sistema de detección
    private Transform currentTarget;
    private int currentPriority = -1;
    private float detectionTimer;
    private Dictionary<string, int> priorityDict;
    private Collider[] detectionBuffer = new Collider[50];

    // Movimiento orgánico
    private float idleTimer;
    private float currentIdleInterval;
    private Vector3 idleTargetPosition;
    private bool isIdleMoving;

    [Header("Animación")]
    [SerializeField] private Animator animator;

    // NavMesh
    private NavMeshAgent agent;

    // Propiedades públicas
    public Transform CurrentTarget => currentTarget;
    public int CurrentPriority => currentPriority;
    public bool HasTarget => currentTarget != null;
    public bool IsMoving => agent != null && agent.velocity.magnitude > 0.1f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"{gameObject.name}: NavMeshAgent no encontrado!");
            enabled = false;
            return;
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;

        priorityDict = new Dictionary<string, int>();
        foreach (var tp in tagPriorities)
        {
            if (!string.IsNullOrEmpty(tp.tag))
            {
                priorityDict[tp.tag] = tp.priority;
            }
        }

        detectionTimer = 0f;
        currentIdleInterval = idleMoveInterval;
    }

    private void Update()
    {
        if (agent == null) return;

        detectionTimer += Time.deltaTime;
        if (detectionTimer >= detectionInterval)
        {
            detectionTimer = 0f;
            DetectTargets();
        }

        if (currentTarget != null)
        {
            MoveTowardsTarget();
        }
        else
        {
            agent.isStopped = true;
        }

        if (animator != null && agent != null)
        {
            animator.SetBool("Moviendo", agent.velocity.magnitude > 0.1f);
        }

        if (agent.velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else if (currentTarget != null)
        {
            Vector3 lookDirection = (currentTarget.position - transform.position).normalized;
            if (lookDirection.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    #region Detección de Objetivos

    private void DetectTargets()
    {
        Vector3 center = transform.position;
        int hitCount = Physics.OverlapSphereNonAlloc(center, detectionRadius, detectionBuffer, detectionLayer);

        Transform bestTarget = null;
        int bestPriority = -1;
        bool currentTargetStillInRange = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = detectionBuffer[i];

            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            if (priorityDict.TryGetValue(col.tag, out int priority))
            {
                if (col.transform == currentTarget)
                {
                    currentTargetStillInRange = true;
                }

                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestTarget = col.transform;
                }
            }
        }

        if (stickyTargeting && currentTarget != null && currentTargetStillInRange)
        {
            if (bestTarget != null && bestPriority > currentPriority)
            {
                Transform oldTarget = currentTarget;
                currentTarget = bestTarget;
                currentPriority = bestPriority;
                OnTargetChanged(oldTarget, currentTarget);
            }
        }
        else if (bestTarget != null)
        {
            if (bestTarget != currentTarget)
            {
                Transform oldTarget = currentTarget;
                currentTarget = bestTarget;
                currentPriority = bestPriority;
                OnTargetChanged(oldTarget, currentTarget);
            }
        }
        else if (currentTarget != null && !currentTargetStillInRange)
        {
            Transform oldTarget = currentTarget;
            currentTarget = null;
            currentPriority = -1;
            OnTargetChanged(oldTarget, null);
        }
    }

    #endregion

    #region Movimiento

    private void MoveTowardsTarget()
    {
        if (currentTarget == null || agent == null)
        {
            agent.isStopped = true;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (maintainDistance)
        {
            float minDistance = optimalDistance - distanceTolerance;
            float maxDistance = optimalDistance + distanceTolerance;

            if (distanceToTarget > maxDistance)
            {
                // Acercarse
                isIdleMoving = false;
                agent.isStopped = false;
                agent.stoppingDistance = optimalDistance - distanceTolerance;
                agent.SetDestination(currentTarget.position);
            }
            else if (distanceToTarget < minDistance)
            {
                // Alejarse - buscar punto válido
                isIdleMoving = false;
                Vector3 retreatPos = CalculateRetreatOrStrafePosition();

                if (retreatPos != Vector3.zero)
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = 0.5f;
                    agent.SetDestination(retreatPos);
                }
                else
                {
                    // No hay escape - quedarse quieto
                    agent.isStopped = true;
                }
            }
            else
            {
                // En rango óptimo
                if (enableIdleMovement)
                {
                    UpdateIdleMovement();
                }
                else
                {
                    agent.isStopped = true;
                }
            }
        }
        else
        {
            agent.isStopped = false;
            agent.stoppingDistance = stoppingDistance;
            agent.SetDestination(currentTarget.position);
        }
    }

    private Vector3 CalculateRetreatOrStrafePosition()
    {
        Vector3 directionFromTarget = (transform.position - currentTarget.position).normalized;
        Vector3 rightDirection = Vector3.Cross(Vector3.up, directionFromTarget).normalized;

        // Prioridades: atrás, diagonales, laterales
        Vector3[] directions = {
            directionFromTarget,                                    // Atrás
            (directionFromTarget + rightDirection).normalized,      // Atrás-derecha
            (directionFromTarget - rightDirection).normalized,      // Atrás-izquierda
            rightDirection,                                         // Derecha
            -rightDirection,                                        // Izquierda
            (directionFromTarget + rightDirection * 2).normalized,  // Más lateral derecha
            (directionFromTarget - rightDirection * 2).normalized   // Más lateral izquierda
        };

        float[] distances = { optimalDistance, optimalDistance * 0.8f, optimalDistance * 0.6f };

        foreach (float dist in distances)
        {
            foreach (Vector3 dir in directions)
            {
                float currentDist = GetDistanceToTarget();
                Vector3 candidatePos = transform.position + dir * (dist - currentDist + distanceTolerance);

                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        float newDist = Vector3.Distance(hit.position, currentTarget.position);
                        if (newDist >= optimalDistance - distanceTolerance)
                        {
                            return hit.position;
                        }
                    }
                }
            }
        }

        return Vector3.zero;
    }

    #endregion

    #region Movimiento Orgánico

    private void UpdateIdleMovement()
    {
        if (!enableIdleMovement || currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        float minDistance = optimalDistance - distanceTolerance;
        float maxDistance = optimalDistance + distanceTolerance;

        bool inOptimalRange = distanceToTarget >= minDistance && distanceToTarget <= maxDistance;

        if (!inOptimalRange)
        {
            isIdleMoving = false;
            return;
        }

        idleTimer += Time.deltaTime;

        // Verificar si llegó al punto idle
        if (isIdleMoving && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            isIdleMoving = false;
            agent.isStopped = true;
        }

        // Buscar nuevo punto idle
        if (idleTimer >= currentIdleInterval && !isIdleMoving)
        {
            idleTimer = 0f;
            currentIdleInterval = idleMoveInterval + Random.Range(-idleMoveIntervalVariance, idleMoveIntervalVariance);

            Vector3 newIdlePos = CalculateIdlePosition();
            if (newIdlePos != Vector3.zero)
            {
                idleTargetPosition = newIdlePos;
                agent.isStopped = false;
                agent.stoppingDistance = 0.3f;
                agent.SetDestination(idleTargetPosition);
                isIdleMoving = true;
            }
        }
    }

    private Vector3 CalculateIdlePosition()
    {
        // Ángulo actual del enemigo respecto al jugador
        Vector3 directionToEnemy = (transform.position - currentTarget.position).normalized;
        float currentAngle = Mathf.Atan2(directionToEnemy.x, directionToEnemy.z) * Mathf.Rad2Deg;

        for (int i = 0; i < 10; i++)
        {
            // Solo moverse en un arco limitado desde la posición actual
            float angleOffset = Random.Range(-idleArcAngle, idleArcAngle);
            float angle = (currentAngle + angleOffset) * Mathf.Deg2Rad;

            float distance = Random.Range(optimalDistance - distanceTolerance * 0.5f,
                                          optimalDistance + distanceTolerance * 0.5f);

            Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * distance;
            Vector3 candidatePos = currentTarget.position + offset;

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                float distFromCurrent = Vector3.Distance(transform.position, hit.position);

                // Distancia corta para evitar atravesar al jugador
                if (distFromCurrent <= idleMaxDistance && distFromCurrent >= idleMinDistance)
                {
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        return hit.position;
                    }
                }
            }
        }

        return Vector3.zero;
    }

    #endregion

    #region Eventos y Utilidades

    private void OnTargetChanged(Transform oldTarget, Transform newTarget)
    {
        // Resetear movimiento idle
        isIdleMoving = false;
        idleTimer = 0f;
        currentIdleInterval = idleMoveInterval;

        if (newTarget != null)
        {
            Debug.Log($"{gameObject.name}: Nuevo objetivo detectado - {newTarget.name} (Tag: {newTarget.tag}, Prioridad: {currentPriority})");
        }
        else if (oldTarget != null)
        {
            Debug.Log($"{gameObject.name}: Objetivo perdido - {oldTarget.name}");
        }
    }

    /// <summary>
    /// Obtiene la prioridad de un tag específico
    /// </summary>
    public int GetTagPriority(string tag)
    {
        return priorityDict.TryGetValue(tag, out int priority) ? priority : -1;
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
    /// Verifica si está en rango óptimo del objetivo
    /// </summary>
    public bool IsInOptimalRange()
    {
        if (currentTarget == null) return false;

        float distance = GetDistanceToTarget();
        return distance >= (optimalDistance - distanceTolerance) &&
               distance <= (optimalDistance + distanceTolerance);
    }

    /// <summary>
    /// Fuerza la detección inmediata
    /// </summary>
    public void ForceDetection()
    {
        DetectTargets();
    }

    /// <summary>
    /// Cambia el objetivo manualmente
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null && priorityDict.TryGetValue(newTarget.tag, out int priority))
        {
            currentTarget = newTarget;
            currentPriority = priority;
            Debug.Log($"{gameObject.name}: Objetivo asignado manualmente - {newTarget.name}");
        }
    }

    /// <summary>
    /// Limpia el objetivo actual
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
        currentPriority = -1;
        agent.isStopped = true;
        isIdleMoving = false;
    }

    /// <summary>
    /// Cambia la distancia óptima dinámicamente
    /// </summary>
    public void SetOptimalDistance(float distance, float tolerance)
    {
        optimalDistance = Mathf.Max(0f, distance);
        distanceTolerance = Mathf.Max(0f, tolerance);
    }

    /// <summary>
    /// Cambia el radio de detección
    /// </summary>
    public void SetDetectionRadius(float radius)
    {
        detectionRadius = Mathf.Max(0f, radius);
    }

    /// <summary>
    /// Activa o desactiva el mantenimiento de distancia
    /// </summary>
    public void SetMaintainDistance(bool maintain)
    {
        maintainDistance = maintain;
    }

    /// <summary>
    /// Activa o desactiva el movimiento orgánico
    /// </summary>
    public void SetIdleMovement(bool enable)
    {
        enableIdleMovement = enable;
        if (!enable)
        {
            isIdleMoving = false;
        }
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = currentTarget != null ? new Color(1, 0, 0, 0.3f) : new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (!Application.isPlaying) return;

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);

            if (maintainDistance)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
                Gizmos.DrawWireSphere(currentTarget.position, optimalDistance);
                Gizmos.color = new Color(1, 0.5f, 0, 0.1f);
                Gizmos.DrawWireSphere(currentTarget.position, optimalDistance - distanceTolerance);
                Gizmos.DrawWireSphere(currentTarget.position, optimalDistance + distanceTolerance);
            }
        }

        // Mostrar punto de movimiento idle
        if (isIdleMoving && idleTargetPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(idleTargetPosition, 0.3f);
            Gizmos.DrawLine(transform.position, idleTargetPosition);
        }

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        if (maintainDistance)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, optimalDistance);
        }
    }

    #endregion
}