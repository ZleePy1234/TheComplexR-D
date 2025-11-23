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
    [SerializeField] private bool stickyTargeting = true; // Mantiene el primer objetivo detectado

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    // Sistema de detección
    private Transform currentTarget;
    private int currentPriority = -1;
    private float detectionTimer;
    private Dictionary<string, int> priorityDict;
    private Collider[] detectionBuffer = new Collider[50];

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
        // Obtener NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"{gameObject.name}: NavMeshAgent no encontrado!");
            enabled = false;
            return;
        }

        // Configurar NavMesh
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;

        // Crear diccionario de prioridades
        priorityDict = new Dictionary<string, int>();
        foreach (var tp in tagPriorities)
        {
            if (!string.IsNullOrEmpty(tp.tag))
            {
                priorityDict[tp.tag] = tp.priority;
            }
        }

        detectionTimer = 0f;
    }

    private void Update()
    {
        if (agent == null) return;

        // Sistema de detección
        detectionTimer += Time.deltaTime;
        if (detectionTimer >= detectionInterval)
        {
            detectionTimer = 0f;
            DetectTargets();
        }

        // Mover hacia el objetivo
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

        // Rotación suave hacia la dirección de movimiento
        if (agent.velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else if (currentTarget != null)
        {
            // Si está detenido pero tiene objetivo, mirar hacia él
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

            // Ignorar el propio enemigo
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            // Verificar si el tag tiene prioridad asignada
            if (priorityDict.TryGetValue(col.tag, out int priority))
            {
                // Verificar si el objetivo actual sigue en rango
                if (col.transform == currentTarget)
                {
                    currentTargetStillInRange = true;
                }

                // Buscar el mejor objetivo
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestTarget = col.transform;
                }
            }
        }

        // Sticky targeting: mantener objetivo actual si sigue en rango
        if (stickyTargeting && currentTarget != null && currentTargetStillInRange)
        {
            // Solo cambiar si encontramos uno con MAYOR prioridad
            if (bestTarget != null && bestPriority > currentPriority)
            {
                Transform oldTarget = currentTarget;
                currentTarget = bestTarget;
                currentPriority = bestPriority;
                OnTargetChanged(oldTarget, currentTarget);
            }
            // Mantener el objetivo actual
        }
        else if (bestTarget != null)
        {
            // Nuevo objetivo o el anterior salió del rango
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
            // Objetivo perdido
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
            // Mantener distancia óptima
            float minDistance = optimalDistance - distanceTolerance;
            float maxDistance = optimalDistance + distanceTolerance;

            if (distanceToTarget > maxDistance)
            {
                // Acercarse
                agent.isStopped = false;
                agent.stoppingDistance = optimalDistance - distanceTolerance;
                agent.SetDestination(currentTarget.position);
            }
            else if (distanceToTarget < minDistance)
            {
                // Alejarse - calcular punto de retroceso
                Vector3 awayDirection = (transform.position - currentTarget.position).normalized;
                Vector3 retreatPosition = currentTarget.position + awayDirection * optimalDistance;

                // Verificar que esté en NavMesh
                if (NavMesh.SamplePosition(retreatPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
            }
            else
            {
                // En rango óptimo - detenerse
                agent.isStopped = true;
            }
        }
        else
        {
            // Modo persecución simple sin mantener distancia
            agent.isStopped = false;
            agent.stoppingDistance = stoppingDistance;
            agent.SetDestination(currentTarget.position);
        }
    }

    #endregion

    #region Eventos y Utilidades

    private void OnTargetChanged(Transform oldTarget, Transform newTarget)
    {
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

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Radio de detección
        Gizmos.color = currentTarget != null ? new Color(1, 0, 0, 0.3f) : new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (!Application.isPlaying) return;

        // Línea al objetivo actual
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);

            // Rango óptimo de distancia
            if (maintainDistance)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
                Gizmos.DrawWireSphere(currentTarget.position, optimalDistance);
                Gizmos.color = new Color(1, 0.5f, 0, 0.1f);
                Gizmos.DrawWireSphere(currentTarget.position, optimalDistance - distanceTolerance);
                Gizmos.DrawWireSphere(currentTarget.position, optimalDistance + distanceTolerance);
            }
        }

        // Path del NavMesh
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

        // Área de detección más visible cuando está seleccionado
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, detectionRadius);

        // Mostrar distancia óptima incluso sin target
        if (maintainDistance)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, optimalDistance);
        }
    }

    #endregion
}