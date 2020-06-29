using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class Body
{
    [System.Serializable]
    public struct BodyPhenotype
    {
        [SerializeField]
        private Vector3 scale;
        [SerializeField]
        private Color color;
        //[SerializeField]
        //private ConfigurableJoint joint;

        [SerializeField]
        [Tooltip("The vertex this body is attached to")]
        private int attachedVertex;
        [SerializeField]
        [Tooltip("The mesh renderer the vertex belongs to")]
        private Body attachedTo;
        [SerializeField]
        [Tooltip("The joint limits, XLow, XHigh Y, Z. Y and Z are mirrored, so 180 means 360 coverage.")]
        private Vector4 jointLimits;
        public int bodyPartID;


        public Vector3 Scale { get => scale; set => scale = new Vector3(value[0] > Creature.minBodyScaleValue ? value[0] : Creature.minBodyScaleValue, value[1] > Creature.minBodyScaleValue ? value[1] : Creature.minBodyScaleValue, value[2] > Creature.minBodyScaleValue ? value[2] : Creature.minBodyScaleValue); }
        public int AttachedVertex { get => attachedVertex; set => attachedVertex = value; }
        public Body AttachedTo { get => attachedTo; set => attachedTo = value; }
        public Color Color { get => color; set => color = value; }
        public Vector4 JointLimits { get => jointLimits; set => jointLimits = value; }

        //public ConfigurableJoint Joint { get => joint; set => joint = value; }

        public BodyPhenotype(Vector3 scale, int attachedVertex, Body attachedMesh, Color color, Vector4 jointLimits) : this()
        {
            Scale = scale;
            //Joint = joint;
            AttachedVertex = attachedVertex;
            AttachedTo = attachedMesh;
            Color = color;
            JointLimits = jointLimits;
        }
        public void MutateParameters()
        {
            if (Random.value < SimulationManager.Instance.CurrentMutationChance)
            {
                Scale = Creature.MutateBodyScale(Scale);
            }
            if (Random.value < SimulationManager.Instance.CurrentMutationChance)
            {
                Color = Creature.CreaturePhenotype.MutateColor(Color);
            }            
            if (Random.value < SimulationManager.Instance.CurrentMutationChance)
            {
                AttachedVertex = Creature.MutateModuleLocation(AttachedVertex, AttachedTo.thisMesh);
            }
            if (Random.value < SimulationManager.Instance.CurrentMutationChance)
            {
                JointLimits += new Vector4(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
                //joints are mirrored, so 180 = 360 coverage and 0 is fixed joint
                jointLimits.x = Mathf.Clamp(jointLimits.x, -180,0);
                jointLimits.y = Mathf.Clamp(jointLimits.y, 0,180);
                jointLimits.z = Mathf.Clamp(jointLimits.z, 0,180);
                jointLimits.w = Mathf.Clamp(jointLimits.w, 0,180);
            }
        }

        public bool Equals(BodyPhenotype other)
        {
            if (other == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }
            return bodyPartID.Equals(other.bodyPartID);
        }
        public bool EqualsMeshVertex(BodyPhenotype other)
        {
            if (other == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }
            return (AttachedVertex, AttachedTo) ==
                   (other.AttachedVertex, other.AttachedTo);
        }
        public bool EqualsVertex(BodyPhenotype other)
        {
            if (other == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }
            return AttachedVertex == other.AttachedVertex;
        }
        public override int GetHashCode()
        {
            return (Scale, AttachedVertex, AttachedTo.transform.localScale.magnitude, Color).GetHashCode();
        }

        public static bool operator ==(BodyPhenotype left, BodyPhenotype right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BodyPhenotype left, BodyPhenotype right)
        {
            return !(left == right);
        }
    }
}
