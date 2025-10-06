using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CutoutObject : MonoBehaviour
{
    [SerializeField] private Transform targetObject;
    [SerializeField] private LayerMask wallMask;

    private Camera mainCam;

    [SerializeField] private float cutoutSizeTarget = 0.05f;
    [SerializeField] private float falloffSizeTarget = 0.025f;
    [SerializeField] private float lerpSpeed = 5f;

    /*Tracking en Diccionarios para los tamaños de cutout, fallof y posiciones 
    para los shaders de todos los rendereres que choquen con el raycast */
    private Dictionary<Renderer, float> previousCutoutSizes = new Dictionary<Renderer, float>();
    private Dictionary<Renderer, float> previousFalloffSizes = new Dictionary<Renderer, float>();
    private Dictionary<Renderer, Vector4> previousCutoutPositions = new Dictionary<Renderer, Vector4>();

    void Awake()
    {
        mainCam = GetComponent<Camera>();
    }

    void Update()
    {
        Vector2 cutoutPos = mainCam.WorldToViewportPoint(targetObject.position);
        cutoutPos.y /= (Screen.width / Screen.height);

        Vector4 cutoutPosVec4 = new Vector4(cutoutPos.x, cutoutPos.y, 0, 0);

        Vector3 offset = targetObject.position - transform.position;
        RaycastHit[] hitObjects = Physics.RaycastAll(transform.position, offset, offset.magnitude, wallMask);

        //tracking de los rendereres actualmente golpeados en un hashset
        HashSet<Renderer> currentRenderers = new HashSet<Renderer>();

        /* Agregamos todos los rendereres que fueron golpeados por el raycast a el hashset de rendereres actuales 
        agarramos los renderers y lerpeamos los valores de cutout y falloff y los ponemos en los materiales y por ultimo
        los guardamos en los diccionarios */ 

        for (int i = 0; i < hitObjects.Length; i++)
        {
            Renderer rend = hitObjects[i].collider.GetComponent<Renderer>();
            if (rend == null) continue;
            currentRenderers.Add(rend);

            float lerpedCutout = previousCutoutSizes.ContainsKey(rend)
                ? Mathf.Lerp(previousCutoutSizes[rend], cutoutSizeTarget, Time.deltaTime * lerpSpeed)
                : Mathf.Lerp(0f, cutoutSizeTarget, Time.deltaTime * lerpSpeed);

            float lerpedFalloff = previousFalloffSizes.ContainsKey(rend)
                ? Mathf.Lerp(previousFalloffSizes[rend], falloffSizeTarget, Time.deltaTime * lerpSpeed)
                : Mathf.Lerp(0f, falloffSizeTarget, Time.deltaTime * lerpSpeed);

            Material[] materials = rend.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m].SetVector("_CutoutPos", cutoutPosVec4);
                materials[m].SetFloat("_CutoutSize", lerpedCutout);
                materials[m].SetFloat("_FalloffSize", lerpedFalloff);
            }

            previousCutoutSizes[rend] = lerpedCutout;
            previousFalloffSizes[rend] = lerpedFalloff;
            previousCutoutPositions[rend] = cutoutPosVec4;
        }

        /* Los renderers que fueron golpeados el frame anterior y ya no estan siendo golpeados se les aplica otro lerp para
        regresar sus valores a cero y se actualiza el material con la ultima posicion del cutout y despues de que se regresa todo
        a cero se agregan a una lista para que sean removidos de los diccionarios */
        List<Renderer> toRemove = new List<Renderer>();
        foreach (Renderer rend in new List<Renderer>(previousCutoutSizes.Keys))
        {
            if (!currentRenderers.Contains(rend) && rend != null)
            {
                float lerpedCutout = Mathf.Lerp(previousCutoutSizes[rend], 0f, Time.deltaTime * lerpSpeed);
                float lerpedFalloff = Mathf.Lerp(previousFalloffSizes[rend], 0f, Time.deltaTime * lerpSpeed);

                Vector4 lastCutoutPos = previousCutoutPositions.ContainsKey(rend)
                    ? previousCutoutPositions[rend]
                    : Vector4.zero;

                Material[] materials = rend.materials;
                for (int m = 0; m < materials.Length; m++)
                {
                    materials[m].SetVector("_CutoutPos", lastCutoutPos);
                    materials[m].SetFloat("_CutoutSize", lerpedCutout);
                    materials[m].SetFloat("_FalloffSize", lerpedFalloff);
                }

                previousCutoutSizes[rend] = lerpedCutout;
                previousFalloffSizes[rend] = lerpedFalloff;

                if (lerpedCutout < 0.001f && lerpedFalloff < 0.001f)
                    toRemove.Add(rend);
            }
        }

        // Limpiar los renderers de la lista de toRemove de todos los diccionarios para dejarlos listos para siguientes frames
        foreach (Renderer rend in toRemove)
        {
            previousCutoutSizes.Remove(rend);
            previousFalloffSizes.Remove(rend);
            previousCutoutPositions.Remove(rend);
        }
    }
}