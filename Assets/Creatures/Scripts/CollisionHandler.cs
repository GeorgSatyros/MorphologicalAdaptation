using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    Creature thisCreature;
    private bool isTouchingGround = false;

    public bool IsTouchingGround { get => isTouchingGround; private set => isTouchingGround = value; }

    private void Awake()
    {
        thisCreature = GetComponentInParent<CreatureHandler>().creature;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("edible") && thisCreature)
            thisCreature.Eat(other.GetComponent<Edible>());
    }

    private void OnCollisionEnter(Collision collision)
    {
        Creature otherCreature = null;
        if (collision.collider.CompareTag("creature"))
            otherCreature = collision.transform.GetComponent<Creature>();
        else if (collision.collider.CompareTag("bodyextension"))
            otherCreature = collision.transform.GetComponent<BodyExtension>().creatureController;
        else if (collision.collider.CompareTag("terrain"))
            IsTouchingGround = true;

        if (otherCreature == null || otherCreature == thisCreature)
            return;

        HandleAgentCollision(collision, otherCreature);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("terrain"))
            IsTouchingGround = false;
    }

    private void HandleAgentCollision(Collision collision, Creature otherCreature)
    {
        //case of carnivore eating other creature
        if (!thisCreature.IsHerbivore && thisCreature.MainBodySize > otherCreature.MainBodySize)
            thisCreature.Eat(otherCreature);
        //case of mating
        else if (SimulationManager.Instance.mode != SimulationMode.Optimized && thisCreature.IsMale != otherCreature.IsMale && thisCreature.WantsToMate)
            thisCreature.MateWithCreature(otherCreature);
    }
}
