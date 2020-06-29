using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public partial class Creature : MonoBehaviour
{
    private const float movementPenalizationFactor = 0.005f;

    [Header("Debug")]
    [SerializeField]
    private int creatureGeneration = 0;
    [SerializeField]
    private float fitness = 0;
    [SerializeField]
    private bool isMale = false;
    [SerializeField]
    private bool isHerbivore = true;
    [TextArea(1, 2)]
    public List<string> lineage = new List<string>();

    private float timeWhenSpawned = 0;

    [Header("Tunable Parameters")]
    public CreaturePhenotype creatureParameters = new CreaturePhenotype();

    [SerializeField]
    private GameObject limbPrefab;
    [SerializeField]
    private GameObject sensorPrefab;
    [SerializeField]
    private GameObject bodyExtensionPrefab;
    public static float minBodyScaleValue = 0.05f; //the minimum x,y,z for all bodies


    [HideInInspector]
    public MeshCollider bodyCollider;
    [HideInInspector]
    public MainBody mainBody;
    [HideInInspector]
    public CreatureHandler creatureHandler;

    public bool enableActuators = true;
    public bool pinAgent = false;

    #region Setters/Getters
    public MeshRenderer MainMeshRenderer { get; set; }
    public Rigidbody Rb { get; private set; }
    public float MainBodySize { get => size; private set => size = value; }
    public float GetTotalSize()
    {
        float totalSize = 0;
        foreach (Body body in creatureParameters.bodies)
            totalSize += CalculateSize(body.bodyParameters.Scale);
        return totalSize + MainBodySize;
    }
    public float UpdateGetFitness()
    {
        fitness = SimulationManager.Instance.CalculateCreatureFitness(this);
        return fitness;
    }
    public float GetFitness()
    {
        return fitness;
    }
    public void UpdateFitness()
    {
        fitness = SimulationManager.Instance.CalculateCreatureFitness(this);
    }
    private void SetFitness(float value)
    {
        fitness = value;
    }
    public float GetAverageLimbStrength()
    {
        if (creatureParameters.limbs.Count <= 0)
            return 0;
        else
            return creatureParameters.limbs.Sum(x => (x.limbParameters.Strength) / creatureParameters.limbs.Count);
    }
    public float GetAverageLimbCooldown()
    {
        if (creatureParameters.limbs.Count <= 0)
            return 0;
        else
            return creatureParameters.limbs.Sum(x => x.limbParameters.MovementCooldown) / creatureParameters.limbs.Count;
    }
    public float GetAverageSensorViewDistance()
    {
        if (creatureParameters.sensors.Count <= 0)
            return 0;
        else
            return creatureParameters.sensors.Sum(x => x.sensorParameters.ViewDistance) / creatureParameters.sensors.Count;
    }
    public float GetAverageSensorViewAngle()
    {
        if (creatureParameters.sensors.Count <= 0)
            return 0;
        else
            return creatureParameters.sensors.Sum(x => x.sensorParameters.ViewAngle) / creatureParameters.sensors.Count;
    }
    public float GetAverageCreatureColorIntensity()
    {
        Color.RGBToHSV(GetAverageColor(), out float H, out float S, out float V);
        return V;
    }
    public Color GetCreatureColor()
    {
        return GetAverageColor();
    }

    public void InitializeColor()
    {
        creatureParameters.averageAgentColor = GetAverageColor();
    }
    public bool IsMale { get => isMale; set => isMale = value; }
    public bool IsHerbivore { get => isHerbivore; set => isHerbivore = value; }
    public int CreatureGeneration { get => creatureGeneration; set => creatureGeneration = value; }

    #endregion
    #region Energy
    public float Energy { get => energy; private set => energy = value; }
    public float TotalEnergyGained { get => totalEnergyGained; private set => totalEnergyGained = value; }
    public float TotalEnergyLost { get => totalEnergyLost; private set => totalEnergyLost = value; }
    public float TimeWhenSpawned { get => timeWhenSpawned; set => timeWhenSpawned = value; }

    public void AddEnergy(float value)
    {
        if (Energy + value >= creatureParameters.MaxEnergy) Energy = creatureParameters.MaxEnergy;
        else
        {
            Energy += value;
        }
        TotalEnergyGained += value;
    }
    public void RemoveEnergy(float value, string source)
    {
        if (Energy - value <= 0)
        {
            Energy = 0;
            Die();
        }
        else
        {
            Energy -= value;
        }
        TotalEnergyLost += value;
        if (energyConsumptionDict.ContainsKey(source))
            energyConsumptionDict[source] = energyConsumptionDict[source] + value;
        else
            energyConsumptionDict.Add(source, value);
    }
    public void ResetEnergy()
    {
        Energy = creatureParameters.MaxEnergy;
    }

    //[Range(0, 1)]
    //[Tooltip("The percentage of the maximum energy lost when fighting/consuming other creatures.")]
    private float fightEnergyLossPercentage = 0.1f;
    #endregion
    [Header("Monitor")]
    #region Module/Mutation Parameters
    [SerializeField]
    private float totalEnergyGained;
    [SerializeField]
    private float totalEnergyLost;
    [SerializeField]
    private float energy;
    [SerializeField]
    private float size;
    public List<Creature> visibleCreatures = new List<Creature>();
    public List<Edible> visibleEdibles = new List<Edible>();
    [SerializeField]
    private float distanceToCurrentTarget;
    public Transform moduleParentObj;
    [SerializeField]
    private StringFloatDictionary energyConsumptionDict = new StringFloatDictionary();

    #endregion

    public void Die()
    {
        creatureHandler.gameObject.SetActive(false);
        if (SimulationManager.Instance.mode == SimulationMode.Free)
            Destroy(creatureHandler, SimulationManager.Instance.epochDuration);
    }
    public void Eat(Creature otherCreatureController)
    {
        AddEnergy(otherCreatureController.Energy - (fightEnergyLossPercentage * creatureParameters.MaxEnergy));
        otherCreatureController.Die();
    }
    public void Eat(Edible edible)
    {
        AddEnergy(edible.EnergyContained);
        edible.Die();
        if (SimulationManager.Instance.mode == SimulationMode.Optimized)
            StartCoroutine(SimulationManager.Instance.EnableEdibleRandomly());
    }

    private void Awake()
    {
        InitializeFields();
        Rb = this.GetComponent<Rigidbody>();
        moduleParentObj = gameObject.FindComponentInChildWithTag<Transform>("moduleParent");
    }
    void Start()
    {
        bodyCollider = mainBody.bodyCollider; //not in awake because of ordering
        if (SimulationManager.Instance.currentEpoch > 0 || SimulationManager.Instance.mode == SimulationMode.Free)
            Mutate(); //first so other values are properly set
        SetupCreature();
        StartCoroutine(HandleHeat());
        StartCoroutine(EnableMovementAfterSeconds(5));
        StartCoroutine(HandleSensorPenalization());
    }

    private IEnumerator EnableMovementAfterSeconds(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        canStartMoving = true;
    }

    private void FixedUpdate()
    {
        TakeDecision();

        if (Input.GetKeyDown(KeyCode.Space) && enableActuators)
        {
            foreach (Limb limb in creatureParameters.limbs.ToList())
            {
                if (limb)
                    limb.Move(distanceToCurrentTarget);
            }
        }
    }
    private void InitializeFields()
    {
        creatureHandler = this.transform.GetComponentInParent<CreatureHandler>();
        SetFitness(0);
        CurrentTarget = null;
        CurrentTerrainTarget = Vector3.zero;
        TotalEnergyGained = 0;
        TotalEnergyLost = 0;
        distanceToCurrentTarget = 0;
        hasTarget = false;
        WantsToMate = false;
        canStartMoving = false;
        TimeWhenSpawned = Time.time;
        energyConsumptionDict = new StringFloatDictionary() { };
    }
    public void SetupCreature()
    {
        UpdateModuleMeshes();
        mainBody.SetScale(creatureParameters.MainBodyScale);
        SetModulePositions(); //because we messed with the main body
        IsMale = (Random.Range(0.0f, 1.0f) >= 0.5f ? true : false);
        StartCoroutine(ResetNeedToMateInSeconds(resetMatingSeconds / 2)); //doen't want to mate immediately
        InitializeColor();
        MainBodySize = CalculateSize(creatureParameters.MainBodyScale);
        Energy = creatureParameters.MaxEnergy;
        Rb.mass = MainBodySize;
        CancelAllExistingRBForces();

        if (!isHerbivore)
            SimulationManager.Instance.predatorsExist = true;
    }

    private void UpdateModuleMeshes()
    {
        foreach (Body body in creatureParameters.bodies)
            body.thisMesh.mesh = creatureHandler.BodyExtensionMesh;

        mainBody.thisMesh.mesh = creatureHandler.MainBodyMesh;
    }

    public void CancelAllExistingRBForces()
    {
        foreach (Rigidbody rb in creatureHandler.GetComponentsInChildren<Rigidbody>())
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    public float CalculateSize(Vector3 scale)
    {
        return scale.magnitude * 2;
    }

    private void SetModulePositions()
    {
        RegisterExistingModules();
        foreach (Limb limb in creatureParameters.limbs.ToList())
            limb.SetTransform();
        foreach (Sensor sensor in creatureParameters.sensors.ToList())
            sensor.SetTransform();
        foreach (BodyExtension body in creatureParameters.bodies.ToList())
            body.SetTransform();
    }

    private IEnumerator HandleHeat()
    {
        float secondsToWait = 10;
        while (true)
        {
            yield return new WaitForSeconds(secondsToWait);
            //1 is the ideal temperature. 
            Color averageCreaturecolor = GetAverageColor();
            Color.RGBToHSV(averageCreaturecolor, out float thisH, out float thisS, out float thisV);
            float whitenessOfMat = thisV;
            float darknessOfMat = 1 - thisV;
            float heatIntensity = SimulationManager.Instance.TheSun.heatIntensity;

            if (heatIntensity != 0) //0 is the ideal temperature
            {
                float energyToRemove = heatIntensity > 0 ? Math.Abs(heatIntensity) * whitenessOfMat : Math.Abs(heatIntensity) * darknessOfMat;
                RemoveEnergy(energyToRemove * secondsToWait, "Heat");
            }
        }
    }

    private Color GetAverageColor()
    {
        float rSum = 0;
        float bSum = 0;
        float gSum = 0;

        List<Body> allBodies = GetAllBodies();

        foreach (Body body in allBodies)
        {
            rSum += body.bodyParameters.Color.r;
            bSum += body.bodyParameters.Color.b;
            gSum += body.bodyParameters.Color.g;
        }
        return new Color(rSum / allBodies.Count, gSum / allBodies.Count, bSum / allBodies.Count, 1);
    }

    public void RegisterExistingModules()
    {
        creatureParameters.limbs = creatureHandler.GetComponentsInChildren<Limb>().ToList();
        creatureParameters.sensors = creatureHandler.GetComponentsInChildren<Sensor>().ToList();
        creatureParameters.bodies = creatureHandler.GetComponentsInChildren<BodyExtension>().ToList();
    }

    public List<Body> GetAllBodies()
    {
        //creatureparameters include only bodyextensions, so add the rest
        if (creatureHandler)
            return creatureHandler.GetComponentsInChildren<Body>().ToList();
        else
            return new List<Body>();
    }
    public List<Sensor> GetAllSensors()
    {
        //creatureparameters include only bodyextensions, so add the rest
        if (creatureHandler)
            return creatureHandler.GetComponentsInChildren<Sensor>().ToList();
        else
            return new List<Sensor>();
    }
    public List<Limb> GetAllLimbs()
    {
        if (creatureHandler)
            //creatureparameters include only bodyextensions, so add the rest
            return creatureHandler.GetComponentsInChildren<Limb>().ToList();
        else
            return new List<Limb>();
    }

    private Creature CrossoverCreatureWith(Creature otherParent)
    {
        //turn a copy of one parent into the offspring (giving birth)
        Vector3 spawnPosition = HandleCreatureSpawningLocation();
        GameObject creatureObj = Instantiate(creatureHandler.gameObject, spawnPosition, Quaternion.identity);
        creatureObj.SetActive(true);
        Creature offspring = creatureObj.GetComponent<CreatureHandler>().creature;
        offspring.gameObject.SetActive(true);
        //here offspring is an exact copy of this parent, so we use it instead
        otherParent.RegisterExistingModules();
        offspring.RegisterExistingModules();
        offspring.CreatureGeneration = SimulationManager.Instance.currentEpoch + 1;
        offspring.name = SimulationManager.GenerateName(SimulationManager.creatureNameLength) + offspring.CreatureGeneration;
        otherParent.CrossPhenotypeSinglePoint(ref offspring);

        offspring.transform.position = spawnPosition;
        offspring.transform.rotation = Quaternion.identity;

        return offspring;
    }

    /// <summary>
    /// Generates an offspring from this parent and otherParent without checking gender. 
    /// Gender and whether to spawn it in a random location can be specified.
    /// </summary>
    /// <param name="otherParent">The second Creature parent to breed with.</param>
    /// <param name="isMale">If the offspring will be male or female. Null randomizes the result.</param>
    /// <param name="randomly">Whether to spawn the offspring at a random position or behind the parent.</param>
    public void GenerateSingleOffspring(Creature otherParent, bool? isMale = null, int species = 0)
    {
        Creature instantiatedOffspring = CrossoverCreatureWith(otherParent);
        if (isMale != null)
            instantiatedOffspring.IsMale = (bool)isMale;
        else
            instantiatedOffspring.IsMale = (Random.Range(0.0f, 1.0f) >= 0.5f ? true : false);

        instantiatedOffspring.lineage.Add("Parent1: '" + this.name + "' with fitness: " + this.UpdateGetFitness() + ".\n Parent2: '" + otherParent.name + "' with fitness: " + otherParent.UpdateGetFitness());
        instantiatedOffspring.creatureHandler.creatureSpecies = species;
    }

    private Vector3 HandleCreatureSpawningLocation(int maxPlacementAttempts = 20)
    {
        Vector3 spawnPosition;
        int attempts = 0;

        if (SimulationManager.Instance.mode == SimulationMode.Free)
        {
            float factor = 5f;
            spawnPosition = transform.position + Random.insideUnitSphere.MultiplyElementByElement(new Vector3(1, 0, 1)) * factor + Vector3.up;
            while (Physics.CheckSphere(spawnPosition, 3) || !IsOverTerrain(spawnPosition))
            {
                spawnPosition = transform.position + Random.insideUnitSphere.MultiplyElementByElement(new Vector3(1, 0, 1)) * factor + Vector3.up;
                attempts++;
                if (attempts > maxPlacementAttempts)
                    break;
            }
        }
        else
        {
            spawnPosition = SimulationManager.Instance.GetRandomCreatureSpawnPoint();
        }

        return spawnPosition;
    }

    private static bool IsOverTerrain(Vector3 spawnPosition)
    {
        float globalBoundaryX = SimulationManager.Instance.Terrain.terrainData.bounds.max.x;
        float globalBoundaryZ = SimulationManager.Instance.Terrain.terrainData.bounds.max.z;

        return spawnPosition.x <= globalBoundaryX && spawnPosition.x >= 0
             && spawnPosition.z <= globalBoundaryZ && spawnPosition.z >= 0
             && spawnPosition.y >= 0;
    }

    public static bool IsSameOrSubclass(Type potentialBase, Type potentialDescendant)
    {
        return potentialDescendant.IsSubclassOf(potentialBase) || potentialDescendant == potentialBase;
    }

    #region Mutations
    //The phenotype is changed here and the changes are enforced on Start before the first frame. The exception is scale which is set here.
    public void Mutate()
    {
        RegisterExistingModules();
        MutateMainBody(); //main body always first
        MutateBodyExtensions();
        SetModulePositions(); //place the existing limbs according to possible new scale. Needed for checking later
                              //the rest are put in place after this, during their start.

        //HandleLimbSensorOverlap(); //Module overlap possible here. Handle it by destroying overlapping module randomly.
        MutateSight();
        MutateMovement();
        MutateEndurance();
        RegisterExistingModules();
    }

    public float GetAgentComplexity()
    {
        return GetAllBodies().Count + GetAllLimbs().Count + GetAllSensors().Count;
    }

    #region GenerativeMutations
    public void GenerateSensor()
    {
        Body targetBody = GetRandomBody();
        MeshFilter targetMeshFilter = targetBody.thisMesh;
        (PointInSpace randomVertexPosition, int randomVertex) = GetRandomVertexOnMesh(targetMeshFilter);

        GameObject newSensorObject = Instantiate(sensorPrefab, targetBody.moduleParentObject);
        Sensor thisNewSensor = newSensorObject.GetComponent<Sensor>();
        thisNewSensor.sensorParameters = new Sensor.SensorPhenotype(Random.Range(0.5f, 5.0f), Random.Range(0, 180.0f), randomVertex, targetBody);
        thisNewSensor.SetTransform();
        thisNewSensor.SetID();
        newSensorObject.name = GenerateModuleName("New_Sensor");

        if (IsOtherModuleOnLocation(thisNewSensor))
        {
            thisNewSensor.DestroyThis();
            return;
        }
    }
    private void GenerateSensorClone(Sensor sensorToCopy, Body attachedBody)
    {
        if (IsOtherModuleOnLocation(sensorToCopy))
        {
            return;
        }
        else
        {
            GameObject sensorObj = Instantiate(sensorToCopy.gameObject, attachedBody.moduleParentObject);
            Sensor newSensor = sensorObj.GetComponent<Sensor>();
            newSensor.sensorParameters.AttachedTo = attachedBody;
            newSensor.SetTransform();
            sensorObj.name = GenerateModuleName("InheritedSensor_" + sensorToCopy.creatureController.name);
            creatureParameters.sensors.Add(newSensor);
            //DestroyNearbyModules(newSensor, 0.05f); //TODO: make proximity check depend on limb size
        }
    }
    public void GenerateLimb()
    {
        //get a random point on a circle around the object with size sizeSquared. Then find the closest point to the collider.
        Body targetBody = GetRandomBody();

        if (targetBody.moduleParentObject.GetComponentInChildren<Limb>())
            return; //if limb already exists on body, quit

        MeshFilter targetMeshFilter = targetBody.thisMesh;
        //TODO: relax these conditions
        (PointInSpace randomVertexPosition, int randomVertex) = GetRandomVertexOnMesh(targetMeshFilter, new List<Vector3> { Vector3.forward, Vector3.up, Vector3.down });

        GameObject limbObj = Instantiate(limbPrefab, targetBody.moduleParentObject);
        Limb newLimb = limbObj.GetComponent<Limb>();
        newLimb.limbParameters = new Limb.LimbPhenotype(Random.Range(0.5f, 10f), Random.Range(0.5f, 2.0f), randomVertex, targetBody);
        newLimb.SetTransform();
        newLimb.SetID();
        newLimb.name = GenerateModuleName("New_Limb");

        //DestroyNearbyModules(newLimb, 0.05f);
    }
    private void GenerateLimbClone(Limb limbToCopy, Body attachedBody)
    {
        GameObject limbObj = Instantiate(limbToCopy.gameObject, attachedBody.moduleParentObject);
        Limb newLimb = limbObj.GetComponent<Limb>();
        newLimb.limbParameters.AttachedTo = attachedBody;
        newLimb.SetTransform();
        limbObj.name = GenerateModuleName("InheritedLimb_" + limbToCopy.creatureController.name);
        creatureParameters.limbs.Add(newLimb);
        //DestroyNearbyModules(newLimb, 0.05f); //TODO: make proximity check depend on limb size
    }
    private void GenerateBodyExtension()
    {
        Body targetBody = GetRandomBody();
        MeshFilter targetMeshFilter = targetBody.thisMesh;
        (PointInSpace randomVertexPosition, int randomVertex) = GetRandomVertexOnMesh(targetMeshFilter, new List<Vector3> { Vector3.down, Vector3.forward });

        GameObject bodyObj = Instantiate(bodyExtensionPrefab, creatureHandler.transform);
        BodyExtension newBodyExtension = bodyObj.GetComponent<BodyExtension>();
        newBodyExtension.bodyParameters = new Body.BodyPhenotype(MutateBodyScale(targetBody.bodyParameters.Scale),
            randomVertex, targetBody, creatureParameters.averageAgentColor,
            new Vector4(Random.Range(-180, 0), Random.Range(0, 180), Random.Range(0, 180), Random.Range(0, 180)));

        newBodyExtension.name = GenerateModuleName("New_Body");
        newBodyExtension.SetID();
        newBodyExtension.connectedTo = targetBody.thisRB;
        if (IsOtherModuleOnLocation(newBodyExtension))
        {
            newBodyExtension.DestroyThis();
            return;
        }
        //DestroyNearbyModules(newBodyExtension.transform.position, 0.2f);
    }
    private void GenerateBodyExtrensionClone(BodyExtension bodyToCopy, Body attachedBody)
    {
        if (IsOtherModuleOnLocation(bodyToCopy))
        {
            GetOtherModuleOnLocation(bodyToCopy).name = GenerateModuleName("InheritedBody_" + bodyToCopy.creatureController.name);
            return;
        }
        else
        {
            GameObject bodyObj = Instantiate(bodyToCopy.gameObject, creatureHandler.transform);
            BodyExtension newBodyExtension = bodyObj.GetComponent<BodyExtension>();
            newBodyExtension.bodyParameters.AttachedTo = attachedBody;
            newBodyExtension.connectedTo = attachedBody.thisRB;
            bodyObj.name = GenerateModuleName("InheritedBody_" + bodyToCopy.creatureController.name);
            creatureParameters.bodies.Add(newBodyExtension);
        }
    }
    private Body GetRandomBody()
    {
        int bodyIndex = Random.Range(0, creatureParameters.bodies.Count + 1);
        if (bodyIndex == creatureParameters.bodies.Count)  //pick main body
        {
            return mainBody;
        }
        else
        {
            return creatureParameters.bodies[bodyIndex];
        }
    }
    private bool IsOtherModuleOnLocation(Body other)
    {
        bool isOnSameLocation = false;
        foreach (Body body in creatureParameters.bodies)
        {
            if (body.bodyParameters.EqualsMeshVertex(other.bodyParameters))
            {
                isOnSameLocation = true;
            }
        }
        return isOnSameLocation;
    }
    private Body GetOtherModuleOnLocation(Body other)
    {
        foreach (Body body in creatureParameters.bodies)
        {
            if (body.bodyParameters.EqualsMeshVertex(other.bodyParameters))
            {
                return body;
            }
        }
        return null;
    }
    private bool IsOtherModuleOnLocation(Sensor other)
    {
        bool isOnSameLocation = false;
        foreach (Sensor sensor in creatureParameters.sensors)
        {
            if (sensor.sensorParameters.EqualsMeshVertex(other.sensorParameters))
            {
                isOnSameLocation = true;
            }
        }
        return isOnSameLocation;
    }
    #endregion

    #region HandleModuleProximity
    //private void DestroyNearbyModules(Limb limb, float distance = 0.05f)
    //{
    //    RegisterExistingModules();
    //    List<Limb> limbs = creatureParameters.limbs;
    //    List<Sensor> sensors = creatureParameters.sensors;

    //    limbs.Remove(limb);

    //    //Destroy any limb that has an origin close to this limb
    //    DestroyLimbsNearPosition(limbs, limb.limbParameters.AttachedTo, limb.transform.position, distance);
    //    //destroy sensors near the given limb
    //    DestroySensorsNearPosition(sensors, limb.limbParameters.AttachedTo, limb.transform.position, distance);

    //}
    //private void DestroyNearbyModules(Sensor sensor, float distance = 0.05f)
    //{
    //    RegisterExistingModules();
    //    List<Limb> limbs = creatureParameters.limbs;
    //    List<Sensor> sensors = creatureParameters.sensors;

    //    sensors.Remove(sensor);

    //    //Destroy any limb that has an origin close to this limb
    //    DestroyLimbsNearPosition(limbs, sensor.sensorParameters.AttachedTo, sensor.transform.position, distance);

    //    //destroy sensors near the given limb
    //    DestroySensorsNearPosition(sensors, sensor.sensorParameters.AttachedTo, sensor.transform.position, distance);
    //}
    //private List<Limb> DestroyLimbsNearPosition(List<Limb> limbsToConsider, Body attachedBody, Vector3 position, float distance)
    //{
    //    List<Limb> limbsToDestroy = limbsToConsider.Where(x => Vector3.Distance(x.transform.position, position) <= distance).ToList();
    //    foreach (Limb existingLimb in limbsToDestroy)
    //    {
    //        //if not on the same body, return
    //        if (existingLimb.limbParameters.AttachedTo.Equals(attachedBody))
    //            return limbsToConsider;
    //        //Debug.Log(String.Format("Destroyed limb {0} of creature {1} in position {2} and {3} to make new module.", existingLimb.name,this.name, existingLimb.transform.position, existingLimb.mirroredMesh.transform.position));
    //        existingLimb.gameObject.SetActive(false); //because destroy happens in the next update
    //        limbsToConsider.Remove(existingLimb);
    //        Destroy(existingLimb.gameObject);
    //    }
    //    return limbsToConsider;
    //}
    //private List<Sensor> DestroySensorsNearPosition(List<Sensor> sensorsToConsider, Body attachedBody, Vector3 position, float distance)
    //{
    //    List<Sensor> sensorsToDestroy = sensorsToConsider.Where(x => Vector3.Distance(x.transform.position, position) <= distance).ToList();
    //    foreach (Sensor existingSensor in sensorsToDestroy)
    //    {
    //        //if not on the same body, return
    //        if (existingSensor.sensorParameters.AttachedTo.Equals(attachedBody))
    //            return sensorsToConsider;
    //        //Debug.Log(String.Format("Destroyed limb {0} of creature {1} in position {2} and {3} to make new module.", existingLimb.name,this.name, existingLimb.transform.position, existingLimb.mirroredMesh.transform.position));
    //        existingSensor.gameObject.SetActive(false); //because destroy happens in the next update
    //        sensorsToConsider.Remove(existingSensor);
    //        Destroy(existingSensor.gameObject);
    //    }
    //    return sensorsToConsider;
    //}
    #endregion

    #region MutateFunctions
    private void MutateSight()
    {
        MutateExistingSensors();
        if (Random.value * 2 < SimulationManager.Instance.CurrentMutationChance)
        {
            GenerateSensor();
        }
    }
    private void MutateMovement()
    {
        MutateExistingLimbs();
        if (Random.value < SimulationManager.Instance.CurrentMutationChance)
        {
            GenerateLimb();
        }
    }
    private void MutateMainBody()
    {
        if (Random.value < SimulationManager.Instance.CurrentMutationChance)
        {
            Vector3 newScale = MutateBodyScale(creatureParameters.MainBodyScale);
            creatureParameters.MainBodyScale = newScale;
            mainBody.bodyParameters.Scale = newScale;
            mainBody.transform.localScale = newScale;
        }
        if (Random.value < SimulationManager.Instance.CurrentMutationChance)
        {
            mainBody.bodyParameters.Color = CreaturePhenotype.MutateColor(mainBody.bodyParameters.Color);
            mainBody.SetTransform();
        }
    }
    private void MutateEndurance()
    {
        if (Random.value < SimulationManager.Instance.CurrentMutationChance)
        {
            creatureParameters.MaxEnergy += Random.Range(-this.creatureParameters.MaxEnergy / 2.0f, this.creatureParameters.MaxEnergy / 2.0f);
            //Debug.Log(String.Format("{0} MUTATED! Made new maximum energy: {1}!", this.transform.name, creatureParameters.MaxEnergy));
        }
    }
    private void MutateBodyExtensions()
    {
        MutateExistingBodyExtensions();

        if (Random.value < SimulationManager.Instance.CurrentMutationChance)
        {
            GenerateBodyExtension();
        }
    }
    #endregion
    #region MutateExistingModules
    private void MutateExistingSensors()
    {
        foreach (Sensor sensor in creatureParameters.sensors.ToList())
        {
            sensor.sensorParameters.MutateSensorParameters();
            sensor.SetTransform();
        }
    }
    private void MutateExistingLimbs()
    {
        foreach (Limb limb in creatureParameters.limbs.ToList())
        {
            if (Random.value < SimulationManager.Instance.CurrentMutationChance)
            {
                limb.limbParameters.MutateLimbParameters();
                limb.SetTransform();
            }
        }
    }
    private void MutateExistingBodyExtensions()
    {
        foreach (BodyExtension body in creatureParameters.bodies.ToList())
        {
            body.bodyParameters.MutateParameters();
            body.SetTransform(); //because the above changes size         
        }
    }
    public static int MutateModuleLocation(int currentVertex, MeshFilter meshFilterAttached)
    {
        return Mathf.Clamp(currentVertex + Random.Range(-1, 2), 0, meshFilterAttached.mesh.vertexCount);
    }
    #endregion
    public static Vector3 MutateBodyScale(Vector3 currentScale)
    {
        //can grow or shrink up to its size per generation
        return currentScale + currentScale.MultiplyElementByElement(Random.insideUnitSphere);
    }
    private string GenerateModuleName(string moduleName)
    {
        return moduleName;
    }
    #endregion
    (PointInSpace, int) GetRandomVertexOnMesh(MeshFilter body, List<Vector3> excludedLocalNormals = null)
    {
        Mesh mesh = body.sharedMesh;
        Vector3[] meshPoints = mesh.vertices;
        Vector3[] meshNormals = mesh.normals;
        int tries = 0;

        while (true)
        {
            int rndVal = Random.Range(0, meshPoints.Length);
            //rndVal = 17;
            Vector3 newPointOnMesh = body.transform.TransformPoint(meshPoints[rndVal]); // convert back to worldspace
            Vector3 newLocalNormal = meshNormals[rndVal];
            Vector3 newNormalOnMesh = body.transform.TransformDirection(newLocalNormal);
            if (excludedLocalNormals != null)
            {
                if (!excludedLocalNormals.Contains(newLocalNormal))
                    return (new PointInSpace(newPointOnMesh, newNormalOnMesh), rndVal);
            }
            else
                return (new PointInSpace(newPointOnMesh, newNormalOnMesh), rndVal);

            tries++;
            if (tries > (meshPoints.Length * 3))
            {
                Debug.LogError("CUSTOM ERROR: No vertices outside excludedNormals list! Getting random vertex instead!");
                return (new PointInSpace(newPointOnMesh, newNormalOnMesh), rndVal);
            }
        }
    }

    #region OnDestroy/Enable/Disable
    void OnDestroy()
    {
        if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.UnregisterCreature(this);
            SimulationManager.Instance.ForgetCreature(this);
            if (creatureHandler != null)
                Destroy(creatureHandler.gameObject);
        }
    }
    void OnEnable()
    {
        EnableThis();
    }

    public void EnableThis()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.RegisterCreature(this);
    }

    void OnDisable()
    {
        DisableThis();
    }

    public void DisableThis()
    {
        if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.UnregisterCreature(this);
            SimulationManager.Instance.RetireCreature(this);
        }
    }
    #endregion
}

public struct PointInSpace
{
    public Vector3 position;
    public Vector3 rotation;

    public PointInSpace(Vector3 position, Vector3 rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }
}
