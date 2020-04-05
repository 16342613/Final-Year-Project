using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HEAT : MonoBehaviour
{
    private bool playedPS = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (playedPS == false)
        {
            this.GetComponent<ParticleSystem>().Play();
            GameObject.Destroy(this.transform.gameObject, 1);
            playedPS = true;
        }
    }
}
