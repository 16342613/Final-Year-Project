using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonScript : MonoBehaviour
{
    public GameObject ammo;
    private List<GameObject> firedAmmos = new List<GameObject>();
    private Vector3 aimAt;
    private int frameCount = 0;
    private bool canFire;
    public float power = 50f;

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("Reload", 0f, 0.5f);  //1s delay, repeat every 1s
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this.transform.position = Camera.main.transform.position;

        if (Input.GetMouseButton(0))
        {
            FireCannon();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            DeleteAll();
        }

        frameCount++;

        //DeleteOutOfBounds();
        
    }

    private void Reload()
    {
        canFire = true;
    }

    private void FireCannon()
    {
        if (canFire == false)
        {
            return;
        }

        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(inputRay, out hit))
        {
            aimAt = hit.point;
        }

        GameObject firedAmmo = Instantiate(ammo, transform.position, Quaternion.identity);
        firedAmmo.transform.LookAt(aimAt);

        firedAmmo.GetComponent<Rigidbody>().AddForce(firedAmmo.transform.forward * power);

        firedAmmos.Add(firedAmmo);

        canFire = false;
    }

    private void DeleteOutOfBounds()
    {
        for (int i = 0; i < firedAmmos.Count; i++)
        {
            Ray inputRay = new Ray(Camera.main.transform.position, firedAmmos[i].transform.position - Camera.main.transform.position);
            RaycastHit hit;

            if (Physics.Raycast(inputRay, out hit))
            {
                if (hit.transform.gameObject != firedAmmos[i])
                {
                    Destroy(firedAmmos[i]);
                    firedAmmos.RemoveAt(i);
                }
            }
        }
    }

    private void DeleteAll()
    {
        for (int i = 0; i < firedAmmos.Count; i++)
        {
            Destroy(firedAmmos[i]);
            firedAmmos.RemoveAt(i);
        }
    }
}
