using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Wheel : Limb
{

    public override void Awake()
    {
        base.Awake();
    }
    protected override void Start()
    {
        base.Start();
    }
    private void Update()
    {
        HandleAnimation();
    }

    private void HandleAnimation()
    {
        meshTransform.Rotate(new Vector3(limbParameters.Strength * 360/2 * Time.deltaTime, 0.0f, 0.0f));
        //mirroredMeshTransform.transform.Rotate(new Vector3(limbParameters.Strength * 360/2 * Time.deltaTime, 0.0f, 0.0f));
    }

    public override void SetTransform()
    {
        limbParameters.MovementCooldown = 0;//for display purposes

        SetOnVertex();
        //SetMirrorLimb();
        //transform.localPosition = adjustedLocalPosition;

        meshTransform.localScale = new Vector3(0.1f,0.2f,0.2f) * limbParameters.Strength / 10;
        //mirroredMeshTransform.localScale = new Vector3(0.1f, 0.2f, 0.2f) * limbParameters.Strength / 10;
    }
    //public override void SetMirrorLimb()
    //{
    //    float newX = Math.Abs(this.transform.localPosition.x) * 2;
    //    mirroredMeshTransform.transform.localPosition = new Vector3(newX, meshTransform.localPosition.y, meshTransform.localPosition.z);
    //}
    public override void SetOnVertex()
    {
        PointInSpace pis = GetVertexGlobalPoint();
        transform.position = pis.position;
        meshTransform.rotation = Quaternion.identity;
    }
    public override void Move(float distanceToTarget)
    {
        if (creatureController)
        {
            Vector3 newVelocity = creatureController.transform.forward * limbParameters.Strength / creatureController.Rb.mass;
            if (newVelocity.sqrMagnitude > creatureController.Rb.velocity.sqrMagnitude)
            {
                creatureController.Rb.velocity = newVelocity;
                creatureController.RemoveEnergy(0.05f * creatureController.Rb.velocity.magnitude * creatureController.MainBodySize, "Limb_Movement");
                //Debug.Log("Energy lost for wheel movement: " + 0.05f * creatureController.Rb.velocity.magnitude * creatureController.Size);
            }
        }
    }
}

