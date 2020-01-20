using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectProperties : MonoBehaviour
{
    private int uniqueID;

    // Start is called before the first frame update
    void Start()
    {
        uniqueID = Random.Range(0, 1023);
    }

    public int GetUniqueID()
    {
        return uniqueID;
    }
}
