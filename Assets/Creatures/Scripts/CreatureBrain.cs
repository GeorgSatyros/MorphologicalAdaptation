using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Jobs;

public partial class Creature
{
    private bool hasTarget = false;
    private bool canStartMoving = false; //used to avoid spikes in velocity when spawned and RBs can be unstable

    #region Brain Parameters
    [Header("Brain Parameters")]
    [SerializeField]
    [Tooltip("The percentage of fitness deviation from itself that is acceptable for picking mates.")]
    private float desirabilityMargin = 0.3f;
    [SerializeField]
    private bool wantsToMate;
    [Tooltip("In how many seconds to make the creature available for mating after making offspring.")]
    [SerializeField]
    private float resetMatingSeconds = 15;
    [SerializeField]
    [Tooltip("The percentage of the maximum energy lost at giving birth. Applicable onyl when female.")]
    private float birthEnergyLossPercentage = 0.3f;
    #endregion

    #region UI
    [Header("UI Parameters")]
    [SerializeField]
    private Sprite foundEdibleIcon;
    [SerializeField]
    private Sprite huntingIcon;
    [SerializeField]
    private Sprite matingIcon;
    [SerializeField]
    private Sprite searchingIcon;
    #endregion

    #region Setters/Getters
    public bool WantsToMate { get => wantsToMate; private set => wantsToMate = value; }
    public Transform CurrentTarget { get; private set; }
    public Vector3 CurrentTerrainTarget { get; private set; }
    #endregion
    private void TakeDecision()
    {
        if (!canStartMoving)
            return;

        if (CurrentTarget && CurrentTarget.gameObject.activeInHierarchy)
        {
            MoveTowardsTarget(CurrentTarget.position);
        }
        else
        {
            CurrentTarget = null;
            PickNextTarget();
            Roam();
        }
    }
    private void DebugDrawLineToTarget(Vector3 targetPosition, Color color)
    {
        Vector3 dirToTarget = (targetPosition - transform.position).normalized;
        float dstToTarget = Vector3.Distance(transform.position, targetPosition);
        DebugExtension.DebugArrow(this.transform.position, dirToTarget * dstToTarget, color);
    }
    private void OnDrawGizmosSelected()
    {
        if (CurrentTarget)
            DebugDrawLineToTarget(CurrentTarget.position, Color.green);
        else
            DebugDrawLineToTarget(CurrentTerrainTarget, Color.yellow);
    }
    private bool CheckIfGrounded()
    {
        if (mainBody.collisionHandler.IsTouchingGround)
        {
            return true;          
        }
        foreach (Body body in creatureParameters.bodies)
        {
            if (body.collisionHandler.IsTouchingGround)
            {
                return true;
            }
        }
        return false;
    }

    void UpdateVisibleEntities()
    {
        HashSet<Creature> tempCreatureSet = new HashSet<Creature>();
        HashSet<Edible> tempEdibleSet = new HashSet<Edible>();

        foreach (Sensor sensor in creatureParameters.sensors)
        {
            tempCreatureSet.UnionWith(sensor.observedCreatures);
            tempEdibleSet.UnionWith(sensor.observedEdibles);
        }

        visibleCreatures = new List<Creature>(tempCreatureSet);
        visibleEdibles = new List<Edible>(tempEdibleSet);
    }

    public void Turn(Vector3 target, float force = 1f)
    {
        if (creatureParameters.limbs.Count <= 0)
            return;

        Vector3 targetDelta = target - transform.position;

        //get the angle between transform.forward and target delta
        float angleDiff = Vector3.Angle(transform.forward, targetDelta);

        // get its cross product, which is the axis of rotation to
        // get from one vector to the other
        Vector3 cross = Vector3.Cross(transform.forward, targetDelta);

        // apply torque along that axis according to the magnitude of the angle.
        Vector3 torque = cross * angleDiff * force;
        Rb.AddTorque(torque);
        PenalizeRotation(angleDiff);
    }

    private void PenalizeRotation(float turnAmount)
    {
        if (turnAmount == 0)
            return;

        float movementPenalization = (MainBodySize + Mathf.Abs(turnAmount)) * movementPenalizationFactor;
        //Debug.Log("Lost energy for turning:" + movementPenalization);
        RemoveEnergy(movementPenalization, "Turning_Movement");
    }
    IEnumerator HandleSensorPenalization()
    {
        //penalize the computational complexity of looking and processing the input
        int numOfSensors = creatureParameters.sensors.Count;
        float randomizedWaitTime = 10 + UnityEngine.Random.Range(-9f, 9f);
        while (true)
        {
            yield return new WaitForSeconds(randomizedWaitTime);
            //Debug.Log("Removed energy due to sensors complexity: "+ (Math.Pow(numOfSensors,2) * 30f) +". Agent name: " + this.name);
            RemoveEnergy((float)Math.Pow(numOfSensors, 2) * randomizedWaitTime, "Input_Processing");
        }
    }
    public void MoveTowardsTarget(Vector3 target)
    {
        if (!enableActuators)
            return;

        if (Energy <= 0 || !CheckIfGrounded())
            return;

        float angle = Vector3.Angle(transform.forward, (target - transform.position).normalized);
        if (angle > 5)
            Turn(target, force: 1f);

        foreach (Limb limb in creatureParameters.limbs.ToList())
        {
            if (limb)
                limb.Move(distanceToCurrentTarget);
        }
        distanceToCurrentTarget = Vector3.Distance(target, this.transform.position);
    }
    private void Roam()
    {
        if (!hasTarget)
        {
            CurrentTerrainTarget = PickRandomNearbyPointOnTerrain(maxDistance: 20, yOffset: creatureParameters.MainBodyScale.y / 2);
            hasTarget = true;
        }
        else
        {
            if (Vector3.Distance(CurrentTerrainTarget, this.transform.position) > MainBodySize)
                MoveTowardsTarget(CurrentTerrainTarget);
            else
                hasTarget = false;
        }
    }
    bool IsHungry()
    {
        return (Energy / creatureParameters.MaxEnergy) < 0.95f;
    }
    public void PickNextTarget()
    {
        UpdateVisibleEntities();
        //if a creature has more than half it's maxEnergy left and sees another creature of an opposite gender with fitness close to it's own
        //it tries to mate. If not and the other creature is smaller, it tries to eat it (if available). Finally, it tries to eat any edibles nearby.

        foreach (Creature creature in visibleCreatures)
        {
            if (creature == null || creature == this)
                continue;

            if (SimulationManager.Instance.mode == SimulationMode.Free && (IsMale != creature.IsMale) && WantsToMate && creature.WantsToMate)
            {
                bool isOtherCreatureFitEnough = (creature.UpdateGetFitness() + creature.UpdateGetFitness() * desirabilityMargin) >= UpdateGetFitness();
                bool isThisCreatureFitEnough = (UpdateGetFitness() + UpdateGetFitness() * desirabilityMargin) >= creature.UpdateGetFitness(); //to avoid chasing after other uninterested creatures

                if (isOtherCreatureFitEnough && isThisCreatureFitEnough)
                {
                    CurrentTarget = creature.transform;
                    creature.CurrentTarget = this.transform;
                    creatureHandler.imageUI.sprite = matingIcon;
                    return;
                }
            }
            else if (!IsHerbivore && creature.MainBodySize < this.MainBodySize)
            {
                CurrentTarget = creature.transform;
                creatureHandler.imageUI.sprite = huntingIcon;
                return;
            }
            //else if (Vector3.Distance(creature.transform.position, transform.position) < 2)
            //    AvoidObstacle();
        }

        if (/*IsHungry() &&*/ visibleEdibles.Count > 0)
        {
            Edible bestEdible = visibleEdibles.OrderByDescending(x => x.EnergyContained).ToList()[0];
            if (bestEdible)
            {
                CurrentTarget = bestEdible.transform;
                creatureHandler.imageUI.sprite = foundEdibleIcon;
                return;
            }
        }

        creatureHandler.imageUI.sprite = searchingIcon;
    }
    public void MateWithCreature(Creature creature)
    {
        if (!IsMale)
            GenerateSingleOffspring(creature);

        WantsToMate = false;
        StartCoroutine(ResetNeedToMateInSeconds(resetMatingSeconds));
        CurrentTarget = null; //resetting targets
        RemoveEnergy(creatureParameters.MaxEnergy * birthEnergyLossPercentage, "Mating");
        //Debug.Log("Energy lost for mating: "+ creatureParameters.MaxEnergy * birthEnergyLossPercentage);
    }
    private void AvoidObstacle()
    {
        Vector3 target = transform.right + Vector3.forward;
        float angle = Vector3.Angle(transform.forward, (target - transform.position).normalized);
        Turn(target, force: 1f);
        foreach (Limb limb in creatureParameters.limbs.ToList())
        {
            if (limb)
                limb.Move(1);
        }
    }
    private IEnumerator ResetNeedToMateInSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        WantsToMate = true;
        yield break;
    }
    private IEnumerator ResetTargetInSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        hasTarget = false;
        yield break;
    }
    private Vector3 PickRandomNearbyPointOnTerrain(float maxDistance = 10f, float yOffset = 0.5f)
    {
        Vector3 pointOnTerrain = SimulationManager.Instance.GenerateCoordinatesOnTerrainNearPosition(this.transform.position, maxDistance, yOffset);
        //pointOnTerrain += Vector3.up*creatureParameters.Scale.y/2;
        return pointOnTerrain;
    }
}
