using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SniperScript : MonoBehaviour
{
    private Transform player;
    public GameObject ammo;
    public float power;
    public float range;
    private bool canFire;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        InvokeRepeating("Reload", 0f, 4f);
    }

    private void Reload()
    {
        canFire = true;
    }

    void Update()
    {
        if ((Vector3.Distance(this.transform.position, player.position) < range) && (canFire == true))
        {
            Shoot(player);
        }
    }

    private void Shoot(Transform target)
    {
        GameObject firedAmmo = GameObject.Instantiate(ammo, this.transform.TransformPoint(new Vector3(0, 0, -1.25f)), Quaternion.identity);
        firedAmmo.transform.LookAt(target);
        firedAmmo.GetComponent<Rigidbody>().AddForce(firedAmmo.transform.forward * power);

        canFire = false;
    }
}
