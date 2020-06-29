using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FoodLevelManager : LevelManager
{
    public override float CalculateFitness(Creature creature)
    {
        float creatureAge = (Time.time - creature.TimeWhenSpawned);
        if (creature.TotalEnergyLost == 0)
            return 0;

        if (SimulationManager.Instance.mode == SimulationMode.Optimized)
            return creature.TotalEnergyGained/creature.TotalEnergyLost;
        else
            return creatureAge * (creature.TotalEnergyGained == 0 ? 0 : 1) + creature.TotalEnergyGained;
    }

    public override List<Creature> RankByFitness(List<Creature> creatures, bool descending = true )
    {
        if (creatures.Count > 0)
        {
            if(descending)
                creatures = creatures.OrderByDescending(x => x.UpdateGetFitness()).ToList();
            else
                creatures = creatures.OrderBy(x => x.UpdateGetFitness()).ToList();
            return creatures;
        }
        else
        {
            Debug.LogError("ERROR: No creatures in list provided!");
            return null;
        }
    }

    public override float GetEdibleHeight()
    {
        return 0;
    }

    public override void Reset()
    {
    }
}
