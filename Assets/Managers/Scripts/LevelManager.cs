using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LevelManager : MonoBehaviour
{
    public abstract List<Creature> RankByFitness(List<Creature> creatures, bool descending = true);
    public abstract float CalculateFitness(Creature creature);
    public abstract float GetEdibleHeight();
    public abstract void Reset();
}
