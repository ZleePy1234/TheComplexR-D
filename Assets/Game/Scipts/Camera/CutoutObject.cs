using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CutoutObject : MonoBehaviour
{

    [SerializeField] private Transform targetObject;

    [SerializeField] private LayerMask wallMask;

    private Camera mainCam;
    void Awake()
    {
        mainCam = GetComponent<Camera>();
    }

    void Update()
    {
        Vector2 cutoutPos = mainCam.WorldToViewportPoint(targetObject.position);
        cutoutPos.y /= (Screen.width / Screen.height);

        Vector3 offset = targetObject.position - transform.position;
        RaycastHit[] hitObjects = Physics.RaycastAll(transform.position, offset, offset.magnitude, wallMask);

        for (int i = 0; i < hitObjects.Length; i++)
        {
            Material[] materials = hitObjects[i].collider.GetComponent<Renderer>().materials;

            for (int m = 0; m < materials.Length; m++)
            {
                materials[m].SetVector("_CutoutPos", cutoutPos);
                materials[m].SetFloat("_CutoutSize", 0.05f);
                materials[m].SetFloat("_FalloffSize", 0.025f);
            }
        }
    }
}
