using UnityEngine;

public class AnimVelocityProxy : MonoBehaviour
{
    public Animator animator;
    Vector3 lastPos;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        lastPos = transform.position;
    }

    void Update()
    {
        // Reconstruimos la velocidad real
        Vector3 worldVel = (transform.position - lastPos) / Time.deltaTime;
        lastPos = transform.position;

        // Ignorar vertical
        worldVel.y = 0f;

        // Convertimos a espacio local del personaje
        Vector3 local = transform.InverseTransformDirection(worldVel);

        // normalizamos para que no pase de 1
        float mx = Mathf.Clamp(local.x, -1f, 1f);
        float my = Mathf.Clamp(local.z, -1f, 1f);

        animator.SetFloat("MoveX", mx);
        animator.SetFloat("MoveY", my);
    }
}
