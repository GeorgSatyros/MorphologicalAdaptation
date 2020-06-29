using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Body : Module
{
    [HideInInspector]
    public MeshCollider bodyCollider;
    public BodyPhenotype bodyParameters;
    public Rigidbody thisRB;
    public Transform moduleParentObject;
    public MeshFilter thisMesh;
    [HideInInspector]
    public CollisionHandler collisionHandler;

    public override void Awake()
    {
        base.Awake();
        thisMesh = GetMesh();
        bodyCollider = thisMesh.GetComponent<MeshCollider>();
        meshTransform = thisMesh.transform;
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        collisionHandler = GetComponent<CollisionHandler>();
    }
    public override void CheckIfEligible()
    { 
    }
    protected virtual MeshFilter GetMesh()
    {
        return GetComponentInChildren<MeshFilter>();
    }
    protected virtual void Start()
    {
        attachedTo = bodyParameters.AttachedTo;
        meshRenderer.material.color = bodyParameters.Color;
        bodyParameters.bodyPartID = id;
    }
    public virtual void SetScale(Vector3 newScale)
    {
        bodyParameters.Scale = newScale;
        transform.localScale = newScale;
    }
    public virtual Vector3 GetColliderCenter()
    {
        Vector3 centerWorldSpace = this.transform.TransformPoint(bodyCollider.bounds.center);
        return creatureController.transform.InverseTransformPoint(centerWorldSpace);
    }    
    public virtual Vector3 GetColliderSize()
    {
        return this.transform.localScale;
    }
    public override void SetOnVertex()
    {
        throw new NotImplementedException();
    }
    public override PointInSpace GetVertexGlobalPoint()
    {
        Mesh mesh = bodyParameters.AttachedTo.thisMesh.sharedMesh;
        Vector3[] meshPoints = mesh.vertices;
        Vector3[] meshNormals = mesh.normals;

        Vector3 pos = bodyParameters.AttachedTo.meshTransform.TransformPoint(meshPoints[bodyParameters.AttachedVertex]);
        Vector3 rot = bodyParameters.AttachedTo.meshTransform.TransformDirection(meshNormals[bodyParameters.AttachedVertex]);

        return new PointInSpace(pos, rot);
    }

}
