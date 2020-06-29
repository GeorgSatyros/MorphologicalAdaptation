using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edible : MonoBehaviour
{
    [SerializeField]
    private float startingEnergy = 1;
    [SerializeField]
    private float growthPerSecond = 0.1f;
    [SerializeField]
    private int growthAppliedEverySeconds = 10;
    [SerializeField]
    private float energyContained = 0;
    [SerializeField]
    private float maxEnergy = 1000;

    public float EnergyContained { get => energyContained; private set => energyContained = value; }
    private void Awake()
    {
        EnergyContained = startingEnergy;
    }
    void Start()
    {
        transform.tag = "edible";
        SetScale();
        StartCoroutine(GrowEdible());
    }

    private void SetScale()
    {
        transform.localScale = Vector3.one * EnergyContained / 100;
    }

    IEnumerator GrowEdible()
    {
        while (EnergyContained < maxEnergy)
        {
            yield return new WaitForSeconds(growthAppliedEverySeconds);
            EnergyContained += growthPerSecond * 10;
            SetScale();
        }
    }
    public void Die()
    {
        this.gameObject.SetActive(false);
    }
    void OnDestroy()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.UnregisterEdible(this);
    }
    void OnEnable()
    {
        SimulationManager.Instance.RegisterEdible(this);
    }
    private void Reset()
    {
        EnergyContained = startingEnergy;
        SetScale();
    }
    internal void Restart()
    {
        Reset();
        gameObject.SetActive(true);
    }
}
