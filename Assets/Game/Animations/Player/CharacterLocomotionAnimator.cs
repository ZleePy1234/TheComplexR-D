using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class CharacterLocomotionAnimator : MonoBehaviour
{
    [Header("Sources (optional)")]
    public Rigidbody rb;                  // opcional
    public CharacterController cc;        // opcional
    public NavMeshAgent agent;            // opcional

    [Header("Animator")]
    public Animator animator;
    public string paramX = "MoveX";
    public string paramY = "MoveY";
    public float dampTime = 0.08f;

    [Header("Look At (optional)")]
    public bool enableUpperBodyLook = true;
    public Transform upperBodyTransform;  // por ejemplo: columna/spine bone
    public float lookSmooth = 15f;
    public float maxLookAngle = 60f;      // limitar rotaci�n de torso
    public LayerMask groundMask = ~0;     // para raycast del cursor
    public float groundPlaneY = 0f;       // altura del "suelo" (ajusta si tu personaje est� en otra Y)

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        // si no asignaste rb/cc/agent en inspector, intenta localizar
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (cc == null) cc = GetComponent<CharacterController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        Vector3 worldVelocity = GetWorldVelocityFallback();
        // si velocidad es muy peque�a, pasa (0,0)
        Vector3 localVel = transform.InverseTransformDirection(worldVelocity);
        // tomamos solo X (strafe) y Z (forward) en espacio local
        Vector2 move = new Vector2(localVel.x, localVel.z);

        // opcional: normalizar para que BlendTree use -1..1 si quieres
        float mag = move.magnitude;
        Vector2 moveNorm = mag > 1e-3f ? move / Mathf.Max(1f, mag) : Vector2.zero;

        // usa animator
        animator.SetFloat(paramX, moveNorm.x, dampTime, Time.deltaTime);
        animator.SetFloat(paramY, moveNorm.y, dampTime, Time.deltaTime);

        // Look at cursor (solo torso)
        if (enableUpperBodyLook && upperBodyTransform != null)
            UpperBodyLookAtCursor();
    }

    Vector3 GetWorldVelocityFallback()
    {
        // 1) Rigidbody
        if (rb != null) return rb.linearVelocity;

        // 2) CharacterController (has velocity property)
        if (cc != null) return cc.velocity;

        // 3) NavMeshAgent
        if (agent != null) return agent.velocity;

        // 4) Try to find input axes (fallback)
        // Asume ejes Input "Horizontal" y "Vertical" en el espacio del mundo/c�mara
        float hx = Input.GetAxisRaw("Horizontal");
        float hy = Input.GetAxisRaw("Vertical");
        // convertimos a world space usando forward de c�mara (suponiendo movimiento relativo a c�mara)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 camForward = cam.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = cam.transform.right;
            camRight.y = 0f;
            camRight.Normalize();
            Vector3 worldDir = (camForward * hy + camRight * hx);
            return worldDir; // sin escala de velocidad, pero suficiente para anim
        }

        return Vector3.zero;
    }

    void UpperBodyLookAtCursor()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        // intersectamos con plano horizontal a la altura groundPlaneY
        Plane ground = new Plane(Vector3.up, new Vector3(0, groundPlaneY, 0));
        if (ground.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Vector3 dir = hit - upperBodyTransform.position;
            dir.y = 0; // solo rotaci�n yaw (opcional)
            if (dir.sqrMagnitude < 0.0001f) return;

            // limitar �ngulo para evitar torsiones raras
            float angle = Vector3.SignedAngle(transform.forward, dir.normalized, Vector3.up);
            angle = Mathf.Clamp(angle, -maxLookAngle, maxLookAngle);
            Quaternion targetRot = Quaternion.AngleAxis(angle, Vector3.up) * transform.rotation;

            // combinamos para que solo afecte la parte superior: multiplicamos rotaci�n local de upperBody
            upperBodyTransform.rotation = Quaternion.Slerp(upperBodyTransform.rotation, targetRot, Time.deltaTime * lookSmooth);
        }
    }
}
