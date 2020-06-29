using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ConfigurableJoint))]
public class BodyExtension : Body
{
    [HideInInspector]
    public ConfigurableJoint thisJoint;
    public Rigidbody connectedTo;

    public override void Awake()
    {
        base.Awake();

        thisJoint = GetComponent<ConfigurableJoint>();
        thisRB = GetComponent<Rigidbody>();
        moduleParentObject = gameObject.FindComponentInChildWithTag<Transform>("moduleParent");
        thisMesh = GetComponentInChildren<MeshFilter>(); //override the variable for bodyextensions
        meshTransform = thisMesh.transform;
    }
    protected override void Start()
    {     
        base.Start();
        SetupCollider();
        SetTransform();
        CheckIfEligible();
    }
    private void SetupCollider()
    {
        this.bodyCollider.enabled = true;
    }
    public override Vector3 GetColliderCenter()
    {
        //BoxCollider childCollider = meshTransform.GetComponent<BoxCollider>();
        Vector3 centerWorldSpace = meshTransform.TransformPoint(bodyCollider.bounds.center);
        return this.transform.InverseTransformPoint(centerWorldSpace);
    }
    public override Vector3 GetColliderSize()
    {
        return meshTransform.localScale;
    }
    public void SetTransform()
    {
        //always setupjoint after set on vertex due to the positioning of the rigidbodies
        SetupBody();
        CheckIfEligible();
        //MakePhantomModule(); //for debug
        SetOnVertex();
        SetupJoint();
    }
    public override void CheckIfEligible()
    {
        if (bodyParameters.Scale.magnitude <= (Vector3.one * Creature.minBodyScaleValue).magnitude)
            DestroyThis();
    }

    public void DestroyThis()
    {
        creatureController.creatureParameters.bodies.Remove(this);
        this.gameObject.SetActive(false);
        DestroyAttachedModules();
        Destroy(this.gameObject);
    }

    private void DestroyAttachedModules()
    {
        foreach (Limb limb in moduleParentObject.GetComponentsInChildren<Limb>().ToList())
        {
            creatureController.creatureParameters.limbs.Remove(limb);
            limb.gameObject.SetActive(false);
            //Destroy(limb.gameObject);
        }        
        foreach (Sensor sensor in moduleParentObject.GetComponentsInChildren<Sensor>().ToList())
        {
            creatureController.creatureParameters.sensors.Remove(sensor);
            sensor.gameObject.SetActive(false);
            //Destroy(sensor.gameObject);
        }
        foreach(BodyExtension body in creatureController.creatureHandler.GetComponentsInChildren<BodyExtension>().ToList())
        {
            if(body.bodyParameters.AttachedTo.Equals(this))
                body.DestroyThis();
        }
    }
    public void SetupBody()
    {
        meshTransform.localScale = bodyParameters.Scale;
        thisRB.mass = bodyParameters.Scale.magnitude;
        meshRenderer.material.color = bodyParameters.Color;

        if (bodyParameters.Scale.magnitude <= (Vector3.one * 0.2f).magnitude)
            thisRB.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        else
            thisRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
    private void SetupJoint()
    {
        EnforceLimitedMotionJointParameters();

        thisJoint.anchor = Vector3.zero; //the skin of the mesh, needs to be done after scale change
        thisJoint.axis = Vector3.forward; //the connected part is the Z axis
        thisJoint.projectionMode = JointProjectionMode.PositionAndRotation;
        thisJoint.projectionDistance = 0.1f;
        thisJoint.projectionAngle = 10;
        thisJoint.enablePreprocessing = false;
        thisJoint.massScale = 1; //how much to use the mass of this body
        thisJoint.connectedMassScale = 1; //how much of the main body mass to use
        thisJoint.connectedBody = connectedTo;
    }
    public override void SetOnVertex()
    {
        if (bodyParameters.AttachedVertex < 0)//main body is -1
        {
            Debug.LogError("CUSTOM ERROR: Main body set on vertex attempt!!");
            return;
        }

        PointInSpace pis = GetVertexGlobalPoint();
        this.transform.position = pis.position;
        this.transform.rotation = Quaternion.LookRotation(-pis.rotation); 
    }

    private void EnforceHingeJointParameters()
    {
        thisJoint.xMotion = ConfigurableJointMotion.Locked;
        thisJoint.yMotion = ConfigurableJointMotion.Locked;
        thisJoint.zMotion = ConfigurableJointMotion.Locked;
        thisJoint.angularXMotion = ConfigurableJointMotion.Locked;
        thisJoint.angularYMotion = ConfigurableJointMotion.Locked;
        thisJoint.angularZMotion = ConfigurableJointMotion.Locked;
    }    
    private void EnforceLimitedMotionJointParameters()
    {
        thisJoint.xMotion = ConfigurableJointMotion.Locked;
        thisJoint.yMotion = ConfigurableJointMotion.Locked;
        thisJoint.zMotion = ConfigurableJointMotion.Locked;
        thisJoint.angularXMotion = ConfigurableJointMotion.Limited;
        thisJoint.angularYMotion = ConfigurableJointMotion.Limited;
        thisJoint.angularZMotion = ConfigurableJointMotion.Limited;

        SoftJointLimit newLimitY = new SoftJointLimit
        {
            limit = bodyParameters.JointLimits.z,
            contactDistance = 0
        };
        thisJoint.angularYLimit = newLimitY;
        SoftJointLimit newLimitZ = new SoftJointLimit
        {
            limit = bodyParameters.JointLimits.w,
            contactDistance = 0
        };
        thisJoint.angularZLimit = newLimitZ;
        SoftJointLimit newLimitXlow = new SoftJointLimit
        {
            limit = bodyParameters.JointLimits.x,
            contactDistance = 0
        };
        thisJoint.lowAngularXLimit = newLimitXlow;
        SoftJointLimit newLimitXhigh = new SoftJointLimit
        {
            limit = bodyParameters.JointLimits.y,
            contactDistance = 0
        };
        thisJoint.highAngularXLimit = newLimitXhigh;

        if(Math.Abs(thisJoint.lowAngularXLimit.limit - thisJoint.highAngularXLimit.limit) <= 1)
            thisJoint.angularXMotion = ConfigurableJointMotion.Locked;
        if(thisJoint.angularYLimit.limit <= 1)
            thisJoint.angularYMotion = ConfigurableJointMotion.Locked;
        if(thisJoint.angularZLimit.limit <= 1)
            thisJoint.angularZMotion = ConfigurableJointMotion.Locked;

    }

    void OnDrawGizmosSelected()
    {
        if(Application.isPlaying)
            DrawDebugJoint();
    }
    private void DrawDebugJoint()
    {
        Gizmos.color = Color.green;
        Vector3 position = GetVertexGlobalPoint().position;
        Gizmos.DrawLine(transform.position, position);
        Gizmos.DrawCube(position, new Vector3(0.05f, 0.05f, 0.05f));
    }
    protected override GameObject MakePhantomModule()
    {
        GameObject module = Instantiate(this.gameObject, this.transform.parent);
        BodyExtension moduleBd = module.GetComponent<BodyExtension>();
        moduleBd.bodyCollider.enabled = false;
        moduleBd.thisRB.isKinematic = true;
        Destroy(moduleBd.thisJoint);
        Material mat = module.GetComponent<MeshRenderer>().material;
        if (mat == null)
            mat = module.GetComponentInChildren<MeshRenderer>().material;
        mat.color = new Color(1, 0, 0, 0.3f);
        module.name = "Phantom_" + module.name;
        moduleBd.enabled = false;
        
        creatureController.creatureParameters.bodies.Remove(moduleBd);
        DestroyImmediate(moduleBd);
        return module;
    }   
}
