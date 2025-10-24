using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [System.Serializable]
    public class TagPriority
    {
        public string tag;
        [Tooltip("Mayor número = Mayor prioridad")]
        public int priority;
    }

    [Header("Configuración de Detección")]
    [SerializeField] private Vector3 detectionAreaSize = new Vector3(10f, 5f, 10f);
    [SerializeField] private Vector3 detectionAreaOffset = Vector3.zero;
    [SerializeField] private LayerMask detectionLayer = -1;

    [Header("Tags y Prioridades")]
    [SerializeField]
    private List<TagPriority> tagPriorities = new List<TagPriority>
    {
        new TagPriority { tag = "Player", priority = 10 },
        new TagPriority { tag = "Tank", priority = 5 },
        new TagPriority { tag = "Ally", priority = 1 }
    };

    [Header("Configuración de Comportamiento")]
    [SerializeField] private float detectionInterval = 0.2f;
    [SerializeField] private bool drawGizmos = true;

    [Header("Configuración de Movimiento")]
    [SerializeField] private float optimalDistance = 5f;
    [SerializeField] private float distanceTolerance = 1f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float deceleration = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private float stoppingDistance = 0.1f;

    [Header("Comportamiento Orgánico")]
    [SerializeField] private bool useOrganicMovement = true;
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmount = 0.1f;
    [SerializeField] private float sideWaveFrequency = 1.5f;
    [SerializeField] private float sideWaveAmount = 0.3f;

    private Transform currentTarget;
    private int currentPriority = -1;
    private float detectionTimer;
    private Dictionary<string, int> priorityDict;
    private List<Collider> detectedObjects = new List<Collider>();
    private Vector3 currentVelocity = Vector3.zero;
    private float currentSpeed = 0f;
    private float organicTimer = 0f;
    private Vector3 lastPosition;

    public Transform CurrentTarget => currentTarget;
    public int CurrentPriority => currentPriority;
    public float CurrentSpeed => currentSpeed;

    private void Start()
    {
        // Crear diccionario para búsqueda rápida de prioridades
        priorityDict = new Dictionary<string, int>();
        foreach (var tp in tagPriorities)
        {
            if (!string.IsNullOrEmpty(tp.tag))
            {
                priorityDict[tp.tag] = tp.priority;
            }
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        detectionTimer += Time.deltaTime;

        if (detectionTimer >= detectionInterval)
        {
            detectionTimer = 0f;
            DetectTargets();
        }

        // Mover hacia el objetivo si existe
        if (currentTarget != null)
        {
            MoveTowardsTarget();
        }
        else
        {
            // Decelerar si no hay objetivo
            DecelerateMovement();
        }

        // Actualizar timer para movimiento orgánico
        if (useOrganicMovement)
        {
            organicTimer += Time.deltaTime;
        }
    }

    private void DetectTargets()
    {
        detectedObjects.Clear();

        // Detectar todos los objetos en el área
        Vector3 center = transform.position + transform.TransformDirection(detectionAreaOffset);
        Collider[] colliders = Physics.OverlapBox(
            center,
            detectionAreaSize / 2f,
            transform.rotation,
            detectionLayer
        );

        Transform bestTarget = null;
        int bestPriority = -1;

        // Evaluar cada objeto detectado
        foreach (var col in colliders)
        {
            // Ignorar el propio enemigo
            if (col.transform == transform || col.transform.IsChildOf(transform))
                continue;

            detectedObjects.Add(col);

            // Verificar si el tag tiene prioridad asignada
            if (priorityDict.TryGetValue(col.tag, out int priority))
            {
                // Si encontramos un objetivo con mayor prioridad
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestTarget = col.transform;
                }
            }
        }

        // Actualizar objetivo si cambió
        if (bestTarget != currentTarget)
        {
            Transform previousTarget = currentTarget;
            currentTarget = bestTarget;
            currentPriority = bestPriority;

            OnTargetChanged(previousTarget, currentTarget);
        }
        // Si perdimos el objetivo actual
        else if (currentTarget != null && bestTarget == null)
        {
            Transform previousTarget = currentTarget;
            currentTarget = null;
            currentPriority = -1;

            OnTargetChanged(previousTarget, null);
        }
    }

    // Se llama cuando el objetivo cambia    
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

    // Obtiene la prioridad de un tag específico    
    public int GetTagPriority(string tag)
    {
        return priorityDict.TryGetValue(tag, out int priority) ? priority : -1;
    }
    
    // Verifica si hay un objetivo actual    
    public bool HasTarget()
    {
        return currentTarget != null;
    }

    // Obtiene la distancia al objetivo actual    
    public float GetDistanceToTarget()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector3.Distance(transform.position, currentTarget.position);
    }

    // Mueve el enemigo hacia el objetivo manteniendo la distancia óptima   
    private void MoveTowardsTarget()
    {
        if (currentTarget == null) return;

        float distanceToTarget = GetDistanceToTarget();
        Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;

        // Calcular distancia objetivo (óptima)
        float minDistance = optimalDistance - distanceTolerance;
        float maxDistance = optimalDistance + distanceTolerance;

        Vector3 desiredMovement = Vector3.zero;

        // Determinar si necesita moverse
        if (distanceToTarget > maxDistance)
        {
            // Acercarse al objetivo
            desiredMovement = directionToTarget;
        }
        else if (distanceToTarget < minDistance)
        {
            // Alejarse del objetivo
            desiredMovement = -directionToTarget;
        }
        // Si está en rango óptimo, no se mueve lateralmente

        // Aplicar aceleración/deceleración suave
        if (desiredMovement.magnitude > 0.01f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, moveSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // Calcular velocidad con movimiento orgánico
        Vector3 movement = desiredMovement.normalized * currentSpeed;

        if (useOrganicMovement && currentSpeed > 0.1f)
        {
            // Agregar oscilación vertical (bobbing)
            float bobOffset = Mathf.Sin(organicTimer * bobFrequency) * bobAmount * currentSpeed / moveSpeed;
            movement.y += bobOffset;

            // Agregar oscilación lateral solo cuando se está moviendo hacia/desde el objetivo
            Vector3 perpendicular = Vector3.Cross(desiredMovement.normalized, Vector3.up).normalized;
            movement += perpendicular * Mathf.Sin(organicTimer * sideWaveFrequency) * sideWaveAmount * currentSpeed / moveSpeed;
        }

        // Aplicar movimiento
        if (movement.magnitude > stoppingDistance)
        {
            transform.position += movement * Time.deltaTime;
        }

        // Rotar hacia el objetivo
        RotateTowardsTarget(directionToTarget);
    }

    // Rota suavemente hacia el objetivo
    private void RotateTowardsTarget(Vector3 direction)
    {
        if (direction.magnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        if (smoothRotation)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    // Decelera el movimiento cuando no hay objetivo
    private void DecelerateMovement()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);

        if (currentSpeed > 0.01f)
        {
            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
    }

    // Verifica si está en rango óptimo del objetivo
    public bool IsInOptimalRange()
    {
        if (currentTarget == null) return false;

        float distance = GetDistanceToTarget();
        return distance >= (optimalDistance - distanceTolerance) &&
               distance <= (optimalDistance + distanceTolerance);
    }

    // Fuerza la detección inmediata (útil para eventos)
    public void ForceDetection()
    {
        DetectTargets();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Dibujar área de detección
        Gizmos.color = currentTarget != null ? Color.red : Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(detectionAreaOffset, detectionAreaSize);

        // Dibujar línea al objetivo actual
        if (currentTarget != null && Application.isPlaying)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // Dibujar rango de detección con más detalle
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(detectionAreaOffset, detectionAreaSize);
    }
}

