using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;

public partial class Sensor : Module
{
    public LayerMask targetMask;
    private LayerMask _targetMask;
    public LayerMask obstacleMask;
    public float senseEverySeconds = 1f;
    public Color debugFOVColor = Color.white;

    public HashSet<Creature> observedCreatures = new HashSet<Creature>();
    public HashSet<Edible> observedEdibles = new HashSet<Edible>();

    public SensorPhenotype sensorParameters;
    public MeshFilter thisMesh;
    private Collider[] targetsInViewRadius;

    public override void Awake()
    {
        base.Awake();
        thisMesh = GetComponentInChildren<MeshFilter>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshTransform = thisMesh.transform;
    }
    protected virtual void Start()
    {
        SetViewLayers();
        SetTransform();
        CheckIfEligible();
        StartCoroutine(FindTargetsWithDelay());
    }

    private void SetViewLayers()
    {
        if (!SimulationManager.Instance.predatorsExist)
            _targetMask = LayerMask.GetMask("Edible");
    }

    public void DestroyThis()
    {
        creatureController.creatureParameters.sensors.Remove(this);
        Destroy(this.gameObject);
    }
    public override void CheckIfEligible()
    {
        if (sensorParameters.ViewDistance <= 2f || sensorParameters.ViewAngle <= 20f)
        {
            creatureController.creatureParameters.sensors.Remove(this);
            Destroy(this.gameObject);
        }
    }
    public virtual void SetTransform()
    {
        attachedTo = sensorParameters.AttachedTo;
        //sensorParameters.ViewAngle = 120;
        SetSensorShape();
        SetOnVertex();
    }

    public virtual void SetSensorShape()
    {
        meshTransform.localScale = new Vector3(sensorParameters.ViewAngle / 100, sensorParameters.ViewAngle / 100, sensorParameters.ViewDistance / 90 ) * 0.1f;
    }
    protected virtual IEnumerator FindTargetsWithDelay()
    {
        senseEverySeconds += UnityEngine.Random.Range(-0.2f,0.2f); //randomize to avoid spikes
        while (true)
        {
            yield return new WaitForSeconds(senseEverySeconds);
            FindVisibleTargets();
        }
    }

    protected virtual void FindVisibleTargets()
    {
        observedCreatures.Clear();
        observedEdibles.Clear();

        targetsInViewRadius = Physics.OverlapSphere(transform.position, sensorParameters.ViewDistance, _targetMask);

        foreach (Collider collider in targetsInViewRadius)
        {
            Transform target = collider.transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) <= sensorParameters.ViewAngle / 2)
            {
                //RaycastHit hit;
                //if (!Physics.Linecast(transform.position, target.position + Vector3.up * 0.05f, out hit, obstacleMask))
                //{
                switch (target.tag)
                {
                    case "edible":
                        observedEdibles.Add(target.GetComponent<Edible>());
                        break;
                    case "bodyextension": //or statement here
                    case "creature":
                        Creature c = target.GetComponentInParent<CreatureHandler>().creature;
                        if (c != creatureController)
                            observedCreatures.Add(c);
                        break;
                    default:
                        Debug.LogError("WARNING: Uknown entity spotted! Name: " + target.name + " with tag " + target.tag);
                        break;
                }
            }
            //else
            //{
            //	//Debug.DrawRay(transform.position, dirToTarget, Color.red, 0.02f);
            //	Debug.DrawRay(transform.position, transform.forward, Color.white, 0.02f);
            //	Debug.DrawLine(transform.position, target.position, Color.cyan, 0.02f);
            //	Debug.DrawLine(transform.position, hit.point, Color.red, 0.02f);

            //}
            //}
        }
    }

    protected Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
            DrawDebugJoint();

        //Gizmos.DrawWireSphere(this.transform.position, sensorParameters.ViewDistance);

        DebugExtension.DebugCone(this.transform.position, this.transform.forward.normalized * sensorParameters.ViewDistance, Color.white, sensorParameters.ViewAngle / 2);
        foreach (Creature target in observedCreatures)
        {
            if (target == null)
                return;

            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            float dstToTarget = Vector3.Distance(transform.position, target.transform.position);
            DebugExtension.DebugArrow(this.transform.position, dirToTarget * dstToTarget, Color.red);
        }

        foreach (Edible target in observedEdibles)
        {
            if (target == null)
                return;

            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            float dstToTarget = Vector3.Distance(transform.position, target.transform.position);
            DebugExtension.DebugArrow(this.transform.position, dirToTarget * dstToTarget, Color.blue);
        }
    }
    public override void SetOnVertex()
    {
        PointInSpace pis = GetVertexGlobalPoint();
        this.transform.position = pis.position;
        this.transform.rotation = Quaternion.LookRotation(pis.rotation);
    }
    public override PointInSpace GetVertexGlobalPoint()
    {
        Mesh mesh = sensorParameters.AttachedTo.thisMesh.sharedMesh;
        Vector3[] meshPoints = mesh.vertices;
        Vector3[] meshNormals = mesh.normals;

        Vector3 pos = sensorParameters.AttachedTo.meshTransform.TransformPoint(meshPoints[sensorParameters.AttachedVertex]);
        Vector3 rot = sensorParameters.AttachedTo.meshTransform.TransformDirection(meshNormals[sensorParameters.AttachedVertex]);

        return new PointInSpace(pos, rot);
    }

    private void DrawDebugJoint()
    {
        Gizmos.color = Color.green;
        Vector3 position = GetVertexGlobalPoint().position;
        Gizmos.DrawLine(transform.position, position);
        Gizmos.DrawCube(position, new Vector3(0.01f, 0.01f, 0.01f));
    }

    public struct SensorData
    {
        public readonly float3 thisPosition;
        public readonly float3 targetPosition;
        public readonly float3 thisDirForward;
        public readonly float sensorViewAngle;
        public readonly float sensorViewDistance;
        private readonly int targetMask;

        public SensorData(Sensor sensor, Transform target)
        {
            this.thisPosition = sensor.transform.position;
            this.targetPosition = target.position;
            this.thisDirForward = sensor.transform.forward;
            this.sensorViewAngle = sensor.sensorParameters.ViewAngle;
            this.sensorViewDistance = sensor.sensorParameters.ViewDistance;
            this.targetMask = sensor._targetMask.value;
        }

    }
}