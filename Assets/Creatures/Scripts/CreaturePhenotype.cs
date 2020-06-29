using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using TMPro;

public partial class Creature
{
    [System.Serializable]
    public partial struct CreaturePhenotype
    {
        [SerializeField]
        private Vector3 mainBodyScale;
        public Color averageAgentColor;

        [SerializeField]
        [Tooltip("The maximum energy of the creature.")]
        private float maxEnergy;

        public List<Limb> limbs;
        public List<Sensor> sensors;
        public List<BodyExtension> bodies;

        public float MaxEnergy { get => maxEnergy; set => maxEnergy = (value < 0) ? 0 : value; }
        public Vector3 MainBodyScale { get => mainBodyScale; set => mainBodyScale = new Vector3(value[0] > minBodyScaleValue ? value[0] : minBodyScaleValue, value[1] > minBodyScaleValue ? value[1] : minBodyScaleValue, value[2] > minBodyScaleValue ? value[2] : minBodyScaleValue); }

        public CreaturePhenotype(Vector3 scale, float maxEnergy) : this()
        {
            MainBodyScale = scale;
            MaxEnergy = maxEnergy;
        }

        public void Print()
        {
            Debug.Log(string.Format("CreaturePhenotype with scale ({0},{1},{2}) , color ({3},{4},{5}), MaxEnergy {6}", mainBodyScale.x, mainBodyScale.y, mainBodyScale.z, averageAgentColor.r, averageAgentColor.g, averageAgentColor.b, maxEnergy));
        }
        public void PrintModules()
        {
            Debug.Log("Inherited modules: ");
            foreach (Limb limb in limbs)
                Debug.Log(limb.name);
            foreach (Sensor sensor in sensors)
                Debug.Log(sensor.name);

        }
        public void ResetModuleLists()
        {
            limbs = new List<Limb>();
            sensors = new List<Sensor>();
            //do not overwrite if bodies exist due to inheritance of references by instantiate.
            if (bodies == null)
                bodies = new List<BodyExtension>();
        }

        public static Color MutateColor(Color color)
        {
            Color.RGBToHSV(color, out float H, out float S, out float pigment);
            float newPigment = Mathf.Clamp01(pigment + Random.Range(-0.5f, 0.5f));
            Color newColor = Color.HSVToRGB(H, S, newPigment);
            newColor.a = 1;
            return newColor;
        }

        public bool Equals(CreaturePhenotype other)
        {
            if (other == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }
            return (MainBodyScale, averageAgentColor, MaxEnergy, bodies) ==
                   (other.MainBodyScale, other.averageAgentColor, other.MaxEnergy, other.bodies);
        }

        public bool EqualsSpecies(CreaturePhenotype other)
        {
            if (other == null)
            {
                // If it is null then it is not equal to this instance.
                return false;
            }
            //case when speciation is disabled and we always have exactly one species
            if (SimulationManager.Instance.speciationDistance < 0)
                return true; 

            if (Math.Abs(other.bodies.Count - bodies.Count) >= SimulationManager.Instance.speciationDistance)
                return false;

            int differences = 0;
            //if bodies are not the same, different creature
            foreach (BodyExtension body in bodies)
            {
                if (!other.bodies.Any(x => x.bodyParameters.Equals(body.bodyParameters)))
                    differences += 1;
            }
            foreach (BodyExtension body in other.bodies)
            {
                if (!bodies.Any(x => x.bodyParameters.Equals(body.bodyParameters)))
                    differences += 1;
            }

            if (Vector4.Distance(other.averageAgentColor, averageAgentColor) > 0.2f)
                differences++;

            if (differences >= SimulationManager.Instance.speciationDistance)
                return false;
            else
                return true;
        }
        public override int GetHashCode()
        {
            return (MainBodyScale, averageAgentColor, MaxEnergy, bodies).GetHashCode();
        }
        public static bool operator ==(CreaturePhenotype left, CreaturePhenotype right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(CreaturePhenotype left, CreaturePhenotype right)
        {
            return !(left == right);
        }
    }

    public void CrossPhenotypeSinglePoint(ref Creature clonedOffspring)
    {
        //CreaturePhenotype newPhenotype = new CreaturePhenotype();
        if (clonedOffspring == this)
            return;

        if (Random.Range(0, 1.0f) > 0.5f)
            clonedOffspring.creatureParameters.MainBodyScale = this.creatureParameters.MainBodyScale;
        if (Random.Range(0, 1.0f) > 0.5f)
            clonedOffspring.creatureParameters.averageAgentColor = this.creatureParameters.averageAgentColor;
        if (Random.Range(0, 1.0f) > 0.5f)
            clonedOffspring.creatureParameters.MaxEnergy = this.creatureParameters.MaxEnergy;

        //clear list, then add the inherited modules only
        //newPhenotype.ResetModuleLists();
        HandleBodyInheritance(ref clonedOffspring);
        HandleSensorLimbInheritance(ref clonedOffspring);

        clonedOffspring.RegisterExistingModules();
        clonedOffspring.SetModulePositions();
    }
    private void HandleSensorLimbInheritance(ref Creature clonedOffspring)
    {
        foreach (Limb limbThisParent in creatureParameters.limbs.ToList())
        {
            Limb limbOfClone = clonedOffspring.creatureParameters.limbs.FirstOrDefault(x => x.Equals(limbThisParent));
            if (limbOfClone != null)
            {
                if (Random.Range(0, 1.0f) > 0.5f)
                {
                    //Debug.LogWarning(String.Format("Succesful crossover of mutual limb parameters from {0} to {1}!", limbThisParent.creatureController.transform.name, clonedOffspring.name));
                    //replace the current parameters of the same limb with the other parent's parameters
                    limbOfClone.limbParameters.CopyValuesFrom(limbThisParent.limbParameters);
                    limbOfClone.name = "InheritedLimb_" + this.name;
                }
            }
            else
            {
                Body attachedBody = clonedOffspring.creatureParameters.bodies.FirstOrDefault(x => x.Equals(limbThisParent.limbParameters.AttachedTo));
                if (attachedBody != null)
                {
                    //Debug.LogWarning(String.Format("Succesful crossover of new limb from {0} to {1}!", limbThisParent.creatureController.transform.name, clonedOffspring.name));
                    clonedOffspring.GenerateLimbClone(limbThisParent, attachedBody);
                }
            }
        }

        foreach (Sensor sensorThisParent in creatureParameters.sensors.ToList())
        {
            Sensor sensorOtherParent = clonedOffspring.creatureParameters.sensors.FirstOrDefault(x => x.Equals(sensorThisParent));
            if (sensorOtherParent != null)
            {
                if (Random.Range(0, 1.0f) > 0.5f)
                {
                    //Debug.LogWarning(String.Format("Succesful crossover of mutual sensor parameters from {0} to {1}!", sensorThisParent.creatureController.transform.name, clonedOffspring.name));
                    //replace the current parameters of the same limb with the other parent's parameters
                    sensorOtherParent.sensorParameters.CopyValuesFrom(sensorThisParent.sensorParameters);
                    sensorOtherParent.name = "InheritedSensor_" + this.name;
                }
            }
            else
            {
                Body attachedBody = clonedOffspring.creatureParameters.bodies.FirstOrDefault(x => x.Equals(sensorThisParent.sensorParameters.AttachedTo));
                if (attachedBody != null)
                {
                    clonedOffspring.GenerateSensorClone(sensorThisParent, attachedBody);
                    //Debug.LogWarning(String.Format("Succesful crossover of new sensor from {0} to {1}!", sensorThisParent.creatureController.transform.name, clonedOffspring.name));
                }
            }
        }

    }
    private void HandleBodyInheritance(ref Creature clonedOffspring)
    {
        RenameExistingBodies(ref clonedOffspring);
        //other bodies have been instanciated. Try to add extra bodyextensions from this parent on top, if connections exist.
        AddValidBodiesFromThisParent(ref clonedOffspring);

        //randomly delete bodies from the instantiated
        //DeleteBodiesRandomly(ref clonedOffspring);
    }

    private void RenameExistingBodies(ref Creature clonedOffspring)
    {
        foreach(Body existingBody in clonedOffspring.creatureParameters.bodies)          
            existingBody.name = GenerateModuleName("InheritedBody_" + existingBody.creatureController.name);        
    }

    private void AddValidBodiesFromThisParent(ref Creature clonedOffspring)
    {
        foreach (BodyExtension bodyThisParent in creatureParameters.bodies.ToList())
        {
            //if body does not already exist in the offspring
            if (!clonedOffspring.creatureParameters.bodies.Exists(x => x.Equals(bodyThisParent)))
            {
                //if there exists a body to attach to, including the main body, do it. Else discard.
                Body suitableCloneBody = clonedOffspring.GetAllBodies().FirstOrDefault(x => x.Equals(bodyThisParent.bodyParameters.AttachedTo));
                if (suitableCloneBody != null)
                {
                    //instantiate all of them, then delete them randomly below
                    if (Random.Range(0, 1.0f) > 0.5f)
                    {
                        //add the extra bodies
                        //Debug.LogWarning(String.Format("Succesful crossover of body from {0} to {1}!", bodyThisParent.creatureController.transform.name, clonedOffspring.name));
                        clonedOffspring.GenerateBodyExtrensionClone(bodyThisParent, suitableCloneBody);
                    }
                }
            }
        }
    }
    private void DeleteBodiesRandomly(ref Creature clonedOffspring)
    {
        foreach (BodyExtension body in clonedOffspring.creatureParameters.bodies.ToList())
        {
            if (Random.Range(0, 1.0f) > 0.5f)
            {
                //add the extra bodies
                //Debug.LogWarning(String.Format("Succesful crossover of body from {0} to {1}!", bodyThisParent.creatureController.transform.name, clonedOffspring.name));
                body.DestroyThis();
            }
        }
    }
}