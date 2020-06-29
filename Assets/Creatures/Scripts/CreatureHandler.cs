using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[SelectionBase]
public class CreatureHandler : MonoBehaviour
{
    public Creature creature;
    Transform creatureParentObj;
    public int creatureSpecies;
    [Header("Debug")]
    [Tooltip("Test giving birth.")]
    public bool reproduce = false;
    [Tooltip("Test mutations. WARNING: Dramatically increases mutation chance.")]
    public bool mutate = false;
    //[Tooltip("Toggle to make all RBs velocity 0.")]
    //public bool removeAllForces = false;
    public Image imageUI;
    [SerializeField]
    private Mesh mainBodyMesh;
    [SerializeField]
    private Mesh bodyExtensionMesh;

    public Mesh MainBodyMesh { get => mainBodyMesh; set => mainBodyMesh = value; }
    public Mesh BodyExtensionMesh { get => bodyExtensionMesh; set => bodyExtensionMesh = value; }

    private void Awake()
    {
        creature = GetComponentInChildren<Creature>(true); //including inactive
        creatureParentObj = SimulationManager.Instance.creatureParent;
        this.transform.parent = creatureParentObj;
        this.transform.rotation = Quaternion.identity;
        imageUI = gameObject.GetComponentInChildren<Image>();
    }
    private void Start()
    {
        this.name = creature.name + "Handler";
        imageUI.transform.parent.localScale = Vector3.one * Math.Min(creature.MainBodySize,2);

    }
    private void Update()
    {
        if (creature.transform.position.y < -Terrain.activeTerrain.transform.position.y - 10)
        {
            SimulationManager.Instance.numFailedColliderAgents += 1;
            Debug.LogError(string.Format("CUSTOM ERROR: Creature died from fall! Total failed agents: {0}!", SimulationManager.Instance.numFailedColliderAgents));
            creature.Die();
        }
        if (reproduce)
        {
            reproduce = false;
            foreach (Creature creature in SimulationManager.Instance.ActiveCreatures.ToList())
                if (creature != this)
                {
                    creature.GenerateSingleOffspring(creature);
                    break;
                }
        }        
        if (mutate)
        {
            mutate = false;
            SimulationManager.Instance.CurrentMutationChance = 10;
            creature.GenerateSingleOffspring(creature);
            Destroy(this.gameObject);
            this.gameObject.SetActive(false);
        }

        HandleUILocation();
    }
    void HandleUILocation()
    {
        imageUI.transform.parent.position = creature.transform.position + Vector3.up * creature.creatureParameters.MainBodyScale.y + 2*Vector3.up;
        imageUI.transform.parent.localScale = Vector3.zero;
    }

}
