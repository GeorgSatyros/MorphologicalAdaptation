using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainBody : Body
{
    public override void Awake()
    {
        base.Awake();
        creatureController.mainBody = this;
        meshRenderer = GetComponent<MeshRenderer>();
        creatureController.MainMeshRenderer = meshRenderer;
        thisRB = creatureController.Rb;
        bodyParameters.AttachedTo = this;
        meshTransform = thisMesh.transform;
        bodyParameters.AttachedVertex = -1;
        moduleParentObject = creatureController.moduleParentObj;
        collisionHandler = creatureController.GetComponent<CollisionHandler>(); //shadow the base
        //this.bodyCollider.enabled = false;
    }
    protected override void Start()
    {
        base.Start();
    }
    public void SetTransform()
    {
        meshRenderer.material.color = bodyParameters.Color;
    }

    protected override MeshFilter GetMesh()
    {
        return GetComponent<MeshFilter>();
    }
}
