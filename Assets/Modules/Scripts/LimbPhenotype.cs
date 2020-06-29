using System;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class Limb
{

    [System.Serializable]
    public struct LimbPhenotype
    {
        [SerializeField]
        private float strength;
        [SerializeField]
        private float movementCooldown;

        [SerializeField]
        private int attachedVertex;
        [SerializeField]
        private Body attachedTo;

        public float Strength { get => strength; set => strength = (value < 0) ? 0 : value; }
        public float MovementCooldown { get => movementCooldown; set => movementCooldown = (value < 0.3f) ? 0.3f : value; }
        public int AttachedVertex { get => attachedVertex; set => attachedVertex = value; }
        public Body AttachedTo { get => attachedTo; set => attachedTo = value; }

        public LimbPhenotype(float strength, float movementCooldown, int attachedVertex, Body attachedMesh) : this()
        {
            Strength = strength;
            MovementCooldown = movementCooldown;
            AttachedVertex = attachedVertex;
            AttachedTo = attachedMesh;
        }
        public void MutateLimbParameters()
        {
            if (Random.value < SimulationManager.Instance.CurrentMutationChance)
            {
                Strength += Random.Range(-1f, 1f);
            }
            if (Random.value < SimulationManager.Instance.CurrentMutationChance)
            {
                MovementCooldown += Random.Range(-1f, 1f);
            }
            if (Random.value < SimulationManager.Instance.CurrentMutationChance)
            {
                AttachedVertex = Creature.MutateModuleLocation(AttachedVertex, AttachedTo.thisMesh);
            }
        }

        public bool Equals(LimbPhenotype other)
        {
            if (other == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }
            return (Strength, MovementCooldown, AttachedVertex, AttachedTo.transform.localScale) ==
                   (other.Strength, other.MovementCooldown, other.AttachedVertex, other.AttachedTo.transform.localScale);
        }        
        public bool EqualsAttachments(LimbPhenotype other)
        {

            if (other == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }
            return (AttachedVertex, AttachedTo.transform.localScale) ==
                   (other.AttachedVertex, other.AttachedTo.transform.localScale);
        }
        public override int GetHashCode()
        {
            return (Strength, MovementCooldown, AttachedVertex, AttachedTo.transform.localScale.magnitude).GetHashCode();
        }
        public static bool operator ==(LimbPhenotype left, LimbPhenotype right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(LimbPhenotype left, LimbPhenotype right)
        {
            return !(left == right);
        }

        internal void CopyValuesFrom(LimbPhenotype otherLimb)
        {
            Strength = otherLimb.Strength;
            MovementCooldown = otherLimb.movementCooldown;
        }
    }
}

