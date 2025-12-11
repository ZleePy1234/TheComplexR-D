using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class resetAberration : MonoBehaviour
{
    private VolumeProfile volume;
    void Awake()
    {
        volume = GetComponent<VolumeProfile>();
        if(volume.TryGet(out ChromaticAberration chromaComp))
        {
            chromaComp.intensity.value = 0.1f;
        }  
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
