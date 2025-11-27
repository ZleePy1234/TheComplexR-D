using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BeatListenerB : MonoBehaviour
{    
    private Volume targetVolume;
    private Bloom  bloom;
    public float currentIntensity;

    
    private void Awake() //I guess I should put most my refrence grabbing on awake not start
    {
        targetVolume = gameObject.GetComponent<Volume>();
        targetVolume.profile.TryGet<Bloom>(out bloom);
    }

    public void ListenToVolumeChange()
    {
        bloom.intensity.value = 10 + (AudioVisualizeManager.Output_Volume * 10f);

    }
    void Update()
    {
        currentIntensity = bloom.intensity.value;
    }
}
