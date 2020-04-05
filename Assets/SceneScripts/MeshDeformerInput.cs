using UnityEngine;

public class MeshDeformerInput : MonoBehaviour
{

    public float force = 10f;
    public float forceOffset = 0.1f;


    void Start()
    {

    }

    /*void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            HandleInput();
            DrawRay();
        }
    }*/

    void DrawRay()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 50, Color.red);

        //torus.GetComponent<BoundingVolumeHierarchy>().ApplyBVH();
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(inputRay, out hit))
        {
            //MeshDeformer deformer = hit.collider.GetComponent<MeshDeformer>();
            ElasticDeformer deformer = hit.collider.GetComponent<ElasticDeformer>();

            if (deformer)
            {
                Vector3 point = hit.point;
                point += hit.normal * forceOffset;
                //deformer.AddDeformingForce(point, force);
                deformer.RespondToForce(point, force);

                //Debug.Log("Input's point: " + point);
            }
        }
    }
}