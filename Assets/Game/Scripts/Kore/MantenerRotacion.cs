using UnityEngine;

public class MantenerRotacion : MonoBehaviour
{
    private Quaternion rotacionInicial;

    void Awake()
    {        
        rotacionInicial = transform.rotation;
    }

    void LateUpdate()
    {       
        transform.rotation = rotacionInicial;        
    }
}
