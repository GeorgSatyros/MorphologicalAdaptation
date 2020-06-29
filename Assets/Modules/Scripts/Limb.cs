using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Limb : Module
{
    public LimbPhenotype limbParameters;
    public bool IsReady { get; protected set; } = true;
    
    public override void Awake()
    {
        base.Awake();
        if (meshTransform == null)
            meshTransform = transform.GetChild(0);

        meshRenderer = GetComponentInChildren<MeshRenderer>();
    }
    protected virtual void Start()
    {
        meshRenderer.material.color = Color.red;
        SetTransform();
        CheckIfEligible();
    }

    public override void CheckIfEligible()
    {
        if (limbParameters.Strength <= 1f)
        {
            creatureController.creatureParameters.limbs.Remove(this);
            Destroy(this.gameObject);
        }
    }
    public virtual void Move(float distanceToTarget)
    {
        if (IsReady)
        {
            Vector3 fullAvailableForce = creatureController.Rb.transform.forward * limbParameters.Strength * 10;
            //float modifier = Math.Min(1, Math.Max(0.8f, distanceToTarget/10)); //when closer than 10, start slowing down
            Vector3 adjustedForce = fullAvailableForce /** modifier*/; 
            //Debug.DrawRay(this.transform.position, adjustedForce, Color.yellow, limbParameters.MovementCooldown);

            attachedTo.thisRB.AddForceAtPosition(adjustedForce, this.transform.position, ForceMode.Force);
            creatureController.RemoveEnergy(0.05f * adjustedForce.magnitude * attachedTo.thisRB.mass, "Limb_Movement"); //sort of the kinetic energy equation
            //Debug.Log("Energy lost for limb movement: "+ 0.05f * adjustedForce.magnitude * attachedTo.thisRB.mass);
            IsReady = false;
            //animHandler.DoWalkAnimation(limbParameters.MovementCooldown);
            if (creatureController.isActiveAndEnabled)
                StartCoroutine(ResetCooldown());
        }
    }
    public virtual void SetTransform()
    {
        //mesh is scaled, meshChild is animated. This is to prevent scaling artifacts.
        attachedTo = limbParameters.AttachedTo;
        SetOnVertex();
        meshTransform.localScale = Vector3.one * limbParameters.Strength / 100;
    }
    public override void SetOnVertex()
    {
        PointInSpace pis = GetVertexGlobalPoint();
        transform.position = pis.position;
        meshTransform.rotation = Quaternion.LookRotation(-pis.rotation);
    }
    protected IEnumerator ResetCooldown()
    {
        yield return new WaitForSeconds(limbParameters.MovementCooldown);
        IsReady = true;
        yield break;
    }
    public override PointInSpace GetVertexGlobalPoint()
    {
        Mesh mesh = limbParameters.AttachedTo.thisMesh.sharedMesh;
        Vector3[] meshPoints = mesh.vertices;
        Vector3[] meshNormals = mesh.normals;

        Vector3 pos = limbParameters.AttachedTo.meshTransform.TransformPoint(meshPoints[limbParameters.AttachedVertex]);
        Vector3 rot = limbParameters.AttachedTo.meshTransform.TransformDirection(meshNormals[limbParameters.AttachedVertex]);

        return new PointInSpace(pos, rot);
    }
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
            DrawDebugJoint();
    }
    private void DrawDebugJoint()
    {
        Gizmos.color = Color.green;
        Vector3 position = GetVertexGlobalPoint().position;
        Gizmos.DrawLine(transform.position, position);
        Gizmos.DrawCube(position, new Vector3(0.05f, 0.05f, 0.05f));
    }
}
