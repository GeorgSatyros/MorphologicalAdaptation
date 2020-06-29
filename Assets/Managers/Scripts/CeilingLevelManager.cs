using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CeilingLevelManager : FoodLevelManager
{
    public float[] edibleCurriculumHeights;
    public int curriculumsPassed = 0;

    public override float GetEdibleHeight()
    {
        if (SimulationManager.Instance.averageFitnessLastEpoch > 0)
        {
            curriculumsPassed = Mathf.Clamp(Mathf.RoundToInt(((float)SimulationManager.Instance.currentEpoch / (float)SimulationManager.Instance.epochs) * edibleCurriculumHeights.Length),0, edibleCurriculumHeights.Length-1);
            Debug.Log(string.Format("Curriculum {0} succesfully passed!", curriculumsPassed));
        }
        return edibleCurriculumHeights[curriculumsPassed];
    }

    public override void Reset()
    {
        base.Reset();
        curriculumsPassed = 0;
    }
}