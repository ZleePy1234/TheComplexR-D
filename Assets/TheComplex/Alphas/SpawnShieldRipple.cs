using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.VFX;

public class SpawnShieldRipple : MonoBehaviour
{
    public GameObject shieldRipples;
    private VisualEffect shieldRippleVFX;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Bullet")
        {
            var ripples = Instantiate(shieldRipples, transform) as GameObject;
            shieldRippleVFX = ripples.GetComponent<VisualEffect>();
            shieldRippleVFX.SetVector3("SphereCenter", collision.contacts[0].point);

            Destroy(ripples, 2);
        }
    }
}
