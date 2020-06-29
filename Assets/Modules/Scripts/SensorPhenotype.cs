using System;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class Sensor
{
    [System.Serializable]
	public struct SensorPhenotype
	{
		[SerializeField]
		private float viewDistance;
		[SerializeField]
		[Range(0, 360)]
		private float viewAngle;

		[SerializeField]
		private int attachedVertex;
		[SerializeField]
		private Body attachedTo;


		public float ViewDistance { get => viewDistance; set => viewDistance = (value < 0) ? 0 : value; }
		public float ViewAngle { get => viewAngle; set => viewAngle = (value < 0) ? 0 : value; }
		public int AttachedVertex { get => attachedVertex; set => attachedVertex = value; }
		public Body AttachedTo { get => attachedTo; set => attachedTo = value; }

		public SensorPhenotype(float viewDistance, float viewAngle, int attachedVertex, Body attachedMesh) : this()
		{
			ViewDistance = viewDistance;
			ViewAngle = viewAngle;
			AttachedVertex = attachedVertex;
			AttachedTo = attachedMesh;
		}

		public void MutateSensorParameters()
		{
			if (Random.value < SimulationManager.Instance.CurrentMutationChance)
			{
				ViewAngle += Random.Range(-20f, 20f);
			}
			if (Random.value < SimulationManager.Instance.CurrentMutationChance)
			{
				ViewDistance += Random.Range(-10f, 10f);
			}
			if (Random.value < SimulationManager.Instance.CurrentMutationChance)
			{
				AttachedVertex = Creature.MutateModuleLocation(AttachedVertex, AttachedTo.thisMesh);
			}
		}
		public bool EqualsAttachments(SensorPhenotype other)
		{
			if (other == null)
			{
				// If it is null then it is not equal to this instance.
				return false;
			}
			return (AttachedVertex, AttachedTo.transform.localScale) ==
				   (other.AttachedVertex, other.AttachedTo.transform.localScale);
		}
		public bool Equals(SensorPhenotype other)
		{
			if (other == null)
			{
				// If it is null then it is not equal to this instance.
				return false;
			}
			return (ViewAngle, ViewDistance, AttachedVertex, AttachedTo.transform.localScale) ==
				   (other.ViewAngle, other.ViewDistance, other.AttachedVertex, other.AttachedTo.transform.localScale);
		}
		public bool EqualsMeshVertex(SensorPhenotype other)
		{
			if (other == null)
			{
				// If it is null then it is not equal to this instance.
				return false;
			}
			return (AttachedVertex, AttachedTo) ==
				   (other.AttachedVertex, other.AttachedTo);
		}
		public override int GetHashCode()
		{
			return (ViewAngle, ViewDistance, AttachedVertex, AttachedTo.transform.localScale.magnitude).GetHashCode();
		}
		public static bool operator ==(SensorPhenotype left, SensorPhenotype right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(SensorPhenotype left, SensorPhenotype right)
		{
			return !(left == right);
		}

		internal void CopyValuesFrom(SensorPhenotype otherSensor)
		{
			ViewAngle = otherSensor.ViewAngle;
			ViewDistance = otherSensor.ViewDistance;
		}
	}
}
