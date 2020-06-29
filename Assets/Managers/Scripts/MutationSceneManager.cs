using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MutationSceneManager : MonoBehaviour
{
    void Start()
    {
        foreach (Creature creature in SimulationManager.Instance.ActiveCreatures)
        {
            creature.creatureParameters.MaxEnergy = 9999999999;
            creature.ResetEnergy();
        }
    }

    public void MutateCreatures()
    {                    
        foreach (Creature creature in SimulationManager.Instance.SortGetAllCreatures())
        {
            creature.creatureHandler.transform.position = new Vector3(10, 0, 10);
            creature.transform.localPosition = Vector3.zero;
            creature.ResetEnergy();
            creature.creatureHandler.gameObject.SetActive(true);
        }
        SimulationManager.Instance.MutateAllCreatures();
    }
}
