using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretScript : MonoBehaviour
{
    private Transform turretRoot;
    private Transform player;
    public GameObject ammo;
    public float power;
    private List<GameObject> firedAmmos = new List<GameObject>();

    void Start()
    {
        turretRoot = this.transform.root;
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        //turretRoot.Rotate(0, 5, 0);
        turretRoot.LookAt(player);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot(player);
        }
    }

    private void Shoot(Transform target)
    {
        GameObject firedAmmo = GameObject.Instantiate(ammo, this.transform.TransformPoint(new Vector3(0, 0, -1.25f)), Quaternion.identity);
        firedAmmo.transform.LookAt(target);
        firedAmmo.GetComponent<Rigidbody>().AddForce(firedAmmo.transform.forward * power);

        firedAmmos.Add(firedAmmo);
    }
}
