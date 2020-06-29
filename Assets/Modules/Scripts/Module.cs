using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Module : MonoBehaviour
{
    [HideInInspector]
    public Creature creatureController;
    public Transform meshTransform;
    protected Body attachedTo;
    public MeshRenderer meshRenderer;
    [SerializeField]
    protected int id;

    public virtual void Awake()
    {
        creatureController = transform.GetComponentInParent<CreatureHandler>().creature;
    }
    public abstract void CheckIfEligible();


    protected virtual Vector3 GetClosestPointToCollider(Collider collider)
    {
        Vector3 closestPoint = collider.ClosestPointOnBounds(this.transform.position);
        if (Vector3.Distance(this.transform.position, closestPoint) < 0.1f)
            return this.transform.position;
        else if(collider.bounds.Contains(this.transform.position))
        {
            Vector3 tempTransform = this.transform.position + Vector3.back; //set it back a distance of 1
            return collider.ClosestPointOnBounds(tempTransform);
        }
        else
            return closestPoint;
    }
    public void SetID()
    {
        id = SimulationManager.Instance.InnovationNumber;
    }
    public bool Equals(Module other)
    {
        if (id.Equals(other.id))
            return true;
        else
            return false;
    }
    protected virtual (PointInSpace,int) GetClosestVertex(MeshFilter meshFilter)
    {
        Mesh mesh = meshFilter.sharedMesh;
        Vector3 thisPosition = this.transform.position;

        Vector3[] meshPoints = mesh.vertices;
        Vector3[] meshNormals = mesh.normals;
        
        Vector3 closestPointOnMesh = meshFilter.transform.TransformPoint(meshPoints[0]); 
        Vector3 closestNormalOnMesh = meshFilter.transform.TransformDirection(meshNormals[0]);
        float prevDistance = Vector3.Distance(thisPosition, closestPointOnMesh);
        int closestIndex = 0;
        for (int i = 1; i < meshPoints.Length; i++)
        {
            float newDistance = Vector3.Distance(thisPosition, meshFilter.transform.TransformPoint(meshPoints[i]));
            if (newDistance < prevDistance)
            {
                prevDistance = newDistance;
                closestPointOnMesh = meshFilter.transform.TransformPoint(meshPoints[i]); // convert back to worldspace
                closestNormalOnMesh = meshFilter.transform.TransformDirection(meshNormals[i]);
                closestIndex = i;
            } 
        }
        return (new PointInSpace(closestPointOnMesh, closestNormalOnMesh), closestIndex);
    }
    public abstract void SetOnVertex();
    public abstract PointInSpace GetVertexGlobalPoint();

    protected virtual GameObject MakePhantomModule()
    {
        GameObject module = Instantiate(this.gameObject, this.transform.parent);
        Material mat = module.GetComponent<MeshRenderer>().material;
        if(mat==null)
            mat = module.GetComponentInChildren<MeshRenderer>().material;       
        mat.color = new Color(1,0,0,0.3f);
        module.name = "Phantom_" + module.name;
        module.GetComponent <Module> ().enabled = false;
        DestroyImmediate(module);
        return module;
    }
}