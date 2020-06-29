using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEditor;
using Tayx.Graphy;
public class SimulationManager : Singleton<SimulationManager>
{
    [Tooltip("1 is normal time. Increase to make the simulation faster.")]
    private float currentTimeScale = 1;
    [Tooltip("The maximum amount of fast-forward the algorithm is allowed. Only relevant if targetFramerate is set to non-zero.")]
    public float maximumTimescale = 10;
    [Tooltip("The framerate the simulation tries to maintain while maximizing timeScale. If 0, timeScale is used instead.")]
    public int targetFramerate = 20;

    [Tooltip("The maximum duration of each epoch in seconds. Also used to mark epochs on free mode, but does not reset the simulation.")]
    public int epochDuration = 200;
    [Tooltip("The number of successive generations to simulate.")]
    public int epochs = 1;
    [Tooltip("Add creatures to the environment to maintain the population.")]
    public bool useCreaturePadding = true;
    [Tooltip("How many seconds between each data dump to the DB.")]
    public float dataRecordingInterval = 60;
    public bool takeScreenshots = false;

    [Tooltip("Optimized deactivates creaturePadding and data recording interval, as these happen automatically every epoch.")]
    public SimulationMode mode = SimulationMode.Optimized;
    //[Tooltip("How many seconds to wait before spawning the next batch of edibles.")]
    //public float edibleSpawnInterval = 10;

    public GameObject creaturePrefab;
    public int initialPopulation = 10;
    [Tooltip("The y offset when spawning agents.")]
    public float agentSpawnHeightOffset = 2;
    public float creatureSpawnRange = 15f;
    [SerializeField]
    [Tooltip("The maximum chance of mutating. In 'optimized' mode this amount decreases every epoch. In 'free' mode it remains as-is throughout the simulation.")]
    private float startingMutationChance = 0.01f;
    private float currentMutationChance;
    public bool useAgentMinimization = true;
    [Tooltip("When negative, speciation is disabled. This is the deltat parameter in NEAT.")]
    public int speciationDistance = -1;
    public List<CreaturesList> speciesList;

    [Tooltip("Pruning removes all agents with 0 fitness after half the total epoch time.")]
    public bool pruneAgents = true;
    public const int creatureNameLength = 3;
    public Transform creatureParent;
    public Transform creatureSpawnPointParent;
    private Transform[] creatureSpawnPoints;
    public Transform edibleSpawnPointParent;
    private Transform[] edibleSpawnPoints;
    private LevelManager levelManager;

    //edibles
    public List<Edible> ediblePrefabList;
    [Tooltip("The number of edibles to eventually have in the level over time.")]
    public int targetEdibles = 50;
    [Tooltip("The number of edibles to randomly spawn at the start of each epoch.")]
    public int startingEdibles = 100;
    public float edibleSpawnRange = 15f;
    [Tooltip("The number of edibles to randomly spawn at each interval. Used only in Free Simulation Mode.")]
    public int ediblesToSpawnPeriodically = 1;
    public Transform edibleParent;

    //UI
    public Text maxAgentsText;
    public Text maxEdiblesText;
    public Text currentEpochText;
    public Text currentTimestepText;
    public Text averageFitnessText;
    public Text timeScaleText;
    public Text mutationChanceText;

    [SerializeField]
    private List<Creature> activeCreatures = new List<Creature>();
    [SerializeField]
    private List<Creature> inactiveCreatures = new List<Creature>();
    [SerializeField]
    private List<Edible> spawnedEdibles = new List<Edible>();
    private const float minimumMutationChance = 0.01f;
    private Creature bestCreature;
    private bool endCurrentEpoch = false;
    private Coroutine timeHandlerCoroutine;
    [HideInInspector]
    public int numFailedColliderAgents = 0;
    [HideInInspector]
    public float averageFitnessLastEpoch = 0;
    [HideInInspector]
    public int currentEpoch;
    [HideInInspector]
    public ScreenshotCamera screenshotCamera;
    [HideInInspector]
    public bool predatorsExist = false;
    private bool hasRunPreUpdateThisEpoch = false;
    private int innovationNumber = 20; //reserve the first 20 for numbers for init
    public int simulationIndex = 0;
    public int maximumRepeats = 50;
    public bool useBestAgent = true;

    #region Setters/Getters    
    void UpdateMaxAgentsText()
    {
        if (maxAgentsText)
            maxAgentsText.text = "Active Agents: " + ActiveCreatures.Count();
    }
    void UpdateMaxEdiblesText()
    {
        if (maxEdiblesText)
        {
            maxEdiblesText.text = "Total Edibles: " + SpawnedEdibles.Where(x => x.gameObject.activeInHierarchy).ToList().Count();
        }
    }   
    void UpdateBrightnessText()
    {
        if (maxEdiblesText)
        {
            maxEdiblesText.text = "Avg Brightness: " + GetAllCreatures().Sum(x => x.GetAverageCreatureColorIntensity()) / (float)GetAllCreatures().Count;
        }
    }

    void UpdateTimestepText(int currentTimestep)
    {
        if (currentTimestepText)
            currentTimestepText.text = "Timestep: " + currentTimestep + " / " + epochDuration;
    }
    void UpdateCurrentEpochText()
    {
        if (currentTimestepText)
            currentEpochText.text = "Epoch: " + currentEpoch + " / " + epochs;
    }
    void UpdateAverageFitnessText()
    {
        List<Creature> allCreatures = GetAllCreatures();
        if (averageFitnessText)
            averageFitnessText.text = "Avg Fitness: " + allCreatures.Sum(x => x.UpdateGetFitness()) / allCreatures.Count();
    }
    void UpdateTimeScaleText()
    {
        if (timeScaleText)
            timeScaleText.text = "Time Scale: " + CurrentTimeScale;
    }
    void UpdateMutationChanceText()
    {
        if (mutationChanceText)
            mutationChanceText.text = "Mutation Chance: " + String.Format("{0:0.00}", CurrentMutationChance);
    }
    private void AddSpawnedCreature(Creature value)
    {
        ActiveCreatures.Add(value);
    }

    public List<Edible> SpawnedEdibles { get => spawnedEdibles; private set => spawnedEdibles = value; }
    public Terrain Terrain { get; private set; }
    public List<Creature> InactiveCreatures { get => inactiveCreatures; private set => inactiveCreatures = value; }
    public List<Creature> ActiveCreatures { get => activeCreatures; private set => activeCreatures = value; }
    public float CurrentMutationChance { get => currentMutationChance; set => currentMutationChance = (value <= minimumMutationChance ? minimumMutationChance : value); }
    public SunHandler TheSun { get; private set; }
    public float CurrentTimeScale { get => currentTimeScale; set => currentTimeScale = value > 1 ? value : 1; }
    public int InnovationNumber { get => innovationNumber++; private set => innovationNumber = value; }

    public void UnregisterCreature(Creature creature)
    {
        ActiveCreatures.Remove(creature);
        UpdateMaxAgentsText();

        if (ActiveCreatures.Count <= 0)
            endCurrentEpoch = true;
    }
    public void UnregisterEdible(Edible edible)
    {
        SpawnedEdibles.Remove(edible);
        UpdateMaxEdiblesText();
    }
    public void RegisterCreature(Creature creature)
    {
        AddSpawnedCreature(creature);
        UpdateMaxAgentsText();
    }
    public void RegisterEdible(Edible edible)
    {
        if (!SpawnedEdibles.Contains(edible))
        {
            SpawnedEdibles.Add(edible);
            UpdateMaxEdiblesText();
        }
    }
    public void RetireCreature(Creature creature)
    {
        InactiveCreatures.Add(creature);
    }

    internal IEnumerator EnableEdibleRandomly()
    {
        List<Edible> inactiveEdibles = SpawnedEdibles.Where(x => x.gameObject.activeInHierarchy == false).ToList();
        if (inactiveEdibles.Count > 10)
        {
            yield return new WaitForSeconds(Random.Range(25f,35f));
            inactiveEdibles[Random.Range(0, inactiveEdibles.Count)].Restart();
        }
        yield break;
    }

    public void ForgetCreature(Creature creature)
    {
        InactiveCreatures.Remove(creature);
    }
    #endregion
    public override void Awake()
    {
        base.Awake();
        Helper.DeleteStatsFile(); //Delete the csv with the statistics before each simulation run
        SetOnAwake();
    }

    private void InitializeAll()
    {
        currentMutationChance = startingMutationChance;
        activeCreatures = new List<Creature>();
        inactiveCreatures = new List<Creature>();
        spawnedEdibles = new List<Edible>();
        bestCreature = null;
        endCurrentEpoch = false;
        numFailedColliderAgents = 0;
        averageFitnessLastEpoch = 0;
        currentEpoch = 0;
        hasRunPreUpdateThisEpoch = false;
        innovationNumber = 20; //reserve the first 20 for numbers for init
    }
    private void SetOnAwake()
    {
        InitializeAll();

        ActiveCreatures = new List<Creature>();
        InactiveCreatures = new List<Creature>();

        if (Terrain == null)
            Terrain = FindObjectOfType<Terrain>();
        TheSun = FindObjectOfType<SunHandler>();

        Helper.DeleteScreenshots();//Delete the file with the screenshots before each simulation run

        creatureSpawnPoints = creatureSpawnPointParent.GetComponentsInChildren<Transform>().Where(x => x != creatureSpawnPointParent).ToArray(); ;
        edibleSpawnPoints = edibleSpawnPointParent.GetComponentsInChildren<Transform>().Where(x => x != edibleSpawnPointParent).ToArray(); ;
    }

    private void Start()
    {
        SetOnStart();
        //SpeciateAgents(GetAllCreatures());
    }

    private void SetOnStart()
    {
        SetTime();
        if (targetFramerate > 0)
            timeHandlerCoroutine = StartCoroutine(HandleTime());
        screenshotCamera = FindObjectOfType<ScreenshotCamera>();
        levelManager = FindObjectOfType<LevelManager>();
        levelManager.Reset();
        SpawnInitialPopulation();

        if (mode == SimulationMode.Optimized)
        {
            SpawnEdiblesRandomly(startingEdibles);
            StartCoroutine(ResetLevelPeriodically(epochDuration, epochs));
        }
        else
        {
            SpawnEdiblesRandomly(ediblesToSpawnPeriodically);
            if (useCreaturePadding)
                StartCoroutine(HandleAgentPadding());
            StartCoroutine(HandleDataReporting());
            StartCoroutine(SpawnEdiblesPeriodically());
        }
        StartCoroutine(CalculateStatistics());
        if (ActiveCreatures.Count <= 0)
            ActiveCreatures = GameObject.FindObjectsOfType<Creature>().ToList();
        Creature randomCreature = ActiveCreatures.OrderBy(o => o.GetAgentComplexity()).ToList()[0]; //only take the least complex agent
        randomCreature.SetupCreature();
        SaveCreatureAsPrefab(randomCreature.creatureHandler, "StartingCreature");
        bestCreature = randomCreature;
        UpdateMutationChanceText();
    }
    private void Update()
    {
        //run preUpdate once
        if (!hasRunPreUpdateThisEpoch)
        {
            PreUpdate();
        }

        DebugHandleTimeInput();
    }

    private void RestartSimulation()
    {
        simulationIndex++;
        if (simulationIndex >= maximumRepeats)
            EditorApplication.isPlaying = false;

        ClearActiveAgents();
        ClearInactiveAgents();
        DestroyAllCreatures();
        DestroyAllEdibles();
        //DestroyAllCreatures();
        SetOnAwake();
        SetOnStart();
    }

    //Is called once before update, on the first update each epoch
    private void PreUpdate()
    {
        SpeciateAgents(GetAllCreatures());
        hasRunPreUpdateThisEpoch = true;
    }

    IEnumerator TakeScreenshotOfGeneration(int creatureCount)
    {
        //yield return new WaitForSeconds(1);
        List<Creature> creatures = SortGetAllCreatures().Take(creatureCount).ToList();
        foreach (Creature creature in creatures)
        {
            creature.creatureHandler.gameObject.SetActive(true);

            screenshotCamera.TakeScreenshot(creature, distance: 3);
            creature.creatureHandler.gameObject.SetActive(false);
            //yield return new WaitForSeconds(1);
        }
        yield break;
    }

    private void DebugHandleTimeInput()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            CurrentTimeScale += 1;
            SetTime();
            if (timeHandlerCoroutine != null)
                StopCoroutine(timeHandlerCoroutine);
        }
        else if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            CurrentTimeScale -= 1;
            SetTime();
            if (timeHandlerCoroutine != null)
                StopCoroutine(timeHandlerCoroutine);

        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CurrentTimeScale = 1;
            SetTime();
            if (timeHandlerCoroutine != null)
                StopCoroutine(timeHandlerCoroutine);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            CurrentTimeScale = 6;
            SetTime();
            if (timeHandlerCoroutine != null)
                StopCoroutine(timeHandlerCoroutine);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            timeHandlerCoroutine = StartCoroutine(HandleTime());
        }

    }

    //called when a variable changes in the editor
    void OnValidate()
    {
        SetTime();
        if (timeHandlerCoroutine != null)
            StopCoroutine(timeHandlerCoroutine);
    }

    private IEnumerator HandleTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);
            float framerate = GraphyManager.Instance.CurrentFPS;
            if (framerate < targetFramerate - targetFramerate / 3)
            {
                float factor = targetFramerate / framerate;
                ModifyTimeScale(-factor / 2);
            }
            else if (framerate > targetFramerate + targetFramerate / 2)
            {
                float factor = framerate / targetFramerate;
                ModifyTimeScale(factor / 2);
            }
        }
    }

    void SetTime()
    {
        Time.timeScale = CurrentTimeScale;
        UpdateTimeScaleText();
    }
    void ModifyTimeScale(float value)
    {
        if (CurrentTimeScale + value >= 1)
        {
            CurrentTimeScale += value;
            CurrentTimeScale = (CurrentTimeScale >= maximumTimescale ? maximumTimescale : CurrentTimeScale);
        }
        else
        {
            CurrentTimeScale = 1;
        }
        SetTime();
    }

    void OnApplicationQuit()
    {
        SaveCreatureAsPrefab(UpdateGetBestCreature().creatureHandler, "LastCreature");
    }
    //to be called by button
    public void MutateAllCreatures()
    {
        foreach (Creature creature in ActiveCreatures)
        {
            creature.creatureHandler.mutate = true;
        }
    }
    private IEnumerator CalculateStatistics()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);
            UpdateAverageFitnessText();
            UpdateBrightnessText();
        }
    }
    private IEnumerator HandleAgentPadding()
    {
        while (true)
        {
            yield return new WaitForSeconds(30);
            if (ActiveCreatures.Count < initialPopulation / 3)
            {
                int agentsToSpawn = (int)(initialPopulation / 10);
                Debug.Log(String.Format("Spawned {0} new agents!", agentsToSpawn));
                SpawnClonesRandomly(Instance.levelManager.RankByFitness(ActiveCreatures, true).GetRange(0, ActiveCreatures.Count / 2), agentsToSpawn);
            }
        }
    }

    private void PruneUnfitAgents()
    {
        foreach (Creature agent in ActiveCreatures.ToList())
            if (agent.UpdateGetFitness() <= 0)
                agent.Die();
    }
    float CalculateCurrentEnergyInEnvironment()
    {
        float edibleEnergy = spawnedEdibles.Sum(edible => edible.EnergyContained);
        float currentCreatureEnergy = ActiveCreatures.Sum(creature => creature.Energy);
        return edibleEnergy + currentCreatureEnergy;
    }

    private IEnumerator SpawnEdiblesPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);
            if (SpawnedEdibles.Count < targetEdibles)
                SpawnEdiblesRandomly(ediblesToSpawnPeriodically);
        }
    }

    private IEnumerator ResetLevelPeriodically(float seconds, int epochs)
    {
        int numberOfChunks = 10;
        for (int i = 0; i < epochs; i++)
        {
            currentEpoch = i;
            UpdateCurrentEpochText();
            Debug.Log("=======================================================================================");
            Debug.Log("Epoch: " + i);

            int chunkSize = (int)seconds / numberOfChunks;
            for (int chunk = 0; chunk < numberOfChunks; chunk++)
            {
                UpdateTimestepText(chunk * chunkSize);
                if (endCurrentEpoch)
                    break;

                //Remove clearly unfit agents halfway through each epoch. This allows for faster convergence to a decent solution.
                //It also helps with performance.
                if (pruneAgents && chunk >= (int)(numberOfChunks / 2))
                    PruneUnfitAgents();

                yield return new WaitForSeconds(chunkSize);
            }

            ReportData(i);
            if (!useBestAgent)
                bestCreature = null;
            UpdateBestCreature(SortGetAllCreatures());

            if (takeScreenshots && (currentEpoch % 10 == 0 || currentEpoch < 5))
            {
                StartCoroutine(TakeScreenshotOfGeneration(5));
                yield return new WaitForSeconds(2);
            }

            if (i >= epochs)
                break; // keeping agents as they are when the last epochs expires

            HandleMutationchanceUpdate();
            ResetLevel();
            //CurrentTimeScale /= 2; //start off low to avoid spikes in performance
            SetTime();
            endCurrentEpoch = false;
            UpdateAverageFitnessText();
            hasRunPreUpdateThisEpoch = false;
        }
#if UNITY_EDITOR
        //EditorApplication.isPlaying = false;
        RestartSimulation();
#else
      Application.Quit();
#endif
        yield break;
    }

    private void HandleMutationchanceUpdate()
    {
        List<Creature> allCreatures = SortGetAllCreatures();
        float averageCreatureFitness = allCreatures.Sum(x => x.UpdateGetFitness()) / (float)allCreatures.Count;
        //only decrease mutation chance when fitness is above zero (slope found).
        if (averageCreatureFitness > 0)
            CurrentMutationChance *= 0.9f;
        UpdateMutationChanceText();
    }

    public float CalculateCreatureFitness(Creature creature)
    {
        if (speciationDistance < 0)
            return levelManager.CalculateFitness(creature);

        float speciesSize = GetAllCreatures().Where(x => x.creatureHandler.creatureSpecies == creature.creatureHandler.creatureSpecies).ToList().Count;
        if (speciesSize > 0)
            return levelManager.CalculateFitness(creature) / speciesSize;
        else
            return levelManager.CalculateFitness(creature);
    }
    public void SpawnInitialPopulation(string name = null)
    {
        for (int i = 0; i < initialPopulation; i++)
        {
            Vector3 randomCoords = GetRandomCreatureSpawnPoint();

            //Generate the Prefab on the generated position
            GameObject creatureObj = Instantiate(creaturePrefab, randomCoords, Quaternion.identity);
            Creature creature = creatureObj.GetComponentInChildren<Creature>();
            creature.CreatureGeneration = 0;
            creature.name = (name ?? GenerateName(creatureNameLength));
        }
    }
    public void SpawnPopulation(List<Creature> creaturesList, int maxPopulationSize)
    {
        float totalFitness = creaturesList.Sum(item => item.UpdateGetFitness());
        totalFitness = (totalFitness > 1 ? totalFitness : 1); //get around 0 fitness

        //if nothing was accomplished and totalfitness is 0, generate maxpopulationsize creatures from random parents
        List<Creature> rouletteList = new List<Creature>();

        //from each species (treat no species case as 1 species) remove agents that are more complex but less performant than the average
        if(useAgentMinimization)
            RemoveUnfitAgentsFromSpecies();

        //constructing a list with creatures duplicated according to their fitness
        int creaturesRemoved = 0;
        foreach (Creature creature in creaturesList)
        {
            if (creature.creatureHandler.creatureSpecies < 0)
            {
                creaturesRemoved++;
                continue;
            }
            double numOfDuplicates = Math.Ceiling((creature.UpdateGetFitness() / totalFitness) * maxPopulationSize * 10) + 1; //at least 1 entry in list
            for (int i = 0; i < numOfDuplicates; i++)
                rouletteList.Add(creature);
        }
        //stochastic universal sampling
        int step = rouletteList.Count / (maxPopulationSize - creaturesRemoved);
        step = step < 1 ? 1 : step;
        int agentsSpawned = 0;
        for (int i = 0; i + step <= rouletteList.Count; i += step)
        {
            //no speciation means there is always just one species
            List<Creature> possibleMates = rouletteList.Where(x => x.creatureHandler.creatureSpecies == rouletteList[i].creatureHandler.creatureSpecies).ToList();
            Creature chosenMate = possibleMates[Random.Range(0, possibleMates.Count)];
            rouletteList[i].GenerateSingleOffspring(chosenMate, null, chosenMate.creatureHandler.creatureSpecies);
            agentsSpawned++;
            if (agentsSpawned >= maxPopulationSize)
                break;
        }

        //best always stays the same in next epoch
        bestCreature.GenerateSingleOffspring(bestCreature, null);
    }

    private void RemoveUnfitAgentsFromSpecies()
    {
        //remove from each species all agents that have more complexity and equal or less fitness than average
        //removes by setting species to -1

        for (int speciesIndex = 0; speciesIndex < speciesList.Count; speciesIndex++)
        {
            float avgSpeciesFitness = speciesList[speciesIndex].list.Sum(x => x.UpdateGetFitness()) / speciesList.Count;
            float avgSpeciesComplexity = speciesList[speciesIndex].list.Sum(x => x.GetAgentComplexity()) / speciesList.Count;

            foreach (Creature creature in speciesList[speciesIndex].list.ToList())
            {
                if (creature.UpdateGetFitness() <= avgSpeciesFitness && creature.GetAgentComplexity() > avgSpeciesComplexity)
                {
                    creature.creatureHandler.creatureSpecies = -1;
                }
            }
        }
    }

    private void SpeciateAgents(List<Creature> creaturesList)
    {
        //create creature species list
        if (speciationDistance < 0)
            return; //for performance

        speciesList = new List<CreaturesList>();
        CreaturesList defaultSpecies = new CreaturesList(new List<Creature>());
        defaultSpecies.list.Add(creaturesList[0]);
        speciesList.Add(defaultSpecies);

        bool foundSpecies;

        for (int rouletteIndex = 1; rouletteIndex < creaturesList.Count; rouletteIndex++)
        {
            foundSpecies = false;
            for (int speciesIndex = 0; speciesIndex < speciesList.Count; speciesIndex++)
            {
                speciesList[speciesIndex].list[0].RegisterExistingModules();
                creaturesList[rouletteIndex].RegisterExistingModules();
                if (speciesList[speciesIndex].list[0].creatureParameters.EqualsSpecies(creaturesList[rouletteIndex].creatureParameters))
                {
                    //Debug.Log(string.Format("Successfully compared agent {0} with {1} bodies and agent {2} with {3} bodies.", speciesList[speciesIndex].list[0].name, speciesList[speciesIndex].list[0].creatureParameters.bodies.Count, creaturesList[rouletteIndex].name, creaturesList[rouletteIndex].creatureParameters.bodies.Count));
                    creaturesList[rouletteIndex].creatureHandler.creatureSpecies = speciesIndex;
                    speciesList[speciesIndex].list.Add(creaturesList[rouletteIndex]);
                    //Debug.Log("Added agent" + creaturesList[rouletteIndex].name + " to species " + speciesIndex);
                    foundSpecies = true;
                    break;
                }
            }
            if (!foundSpecies)
            {
                creaturesList[rouletteIndex].creatureHandler.creatureSpecies = speciesList.Count;
                speciesList.Add(new CreaturesList(new List<Creature>() { creaturesList[rouletteIndex] }));
                //Debug.Log("Added new species: " + speciesList.Count);
            }
        }
        Debug.Log("There exist " + speciesList.Count + " species total!");
    }

    public void SpawnClonesRandomly(List<Creature> creaturesList, int numberOfOffspring)
    {
        for (int i = 0; i < numberOfOffspring; i++)
        {
            int maleCreatures = creaturesList.Count(c => c.IsMale == true);
            int femaleCreatures = creaturesList.Count() - maleCreatures;
            if (maleCreatures > femaleCreatures)
                creaturesList[Random.Range(0, creaturesList.Count)].GenerateSingleOffspring(creaturesList[Random.Range(0, creaturesList.Count)], false);
            else
                creaturesList[Random.Range(0, creaturesList.Count)].GenerateSingleOffspring(creaturesList[Random.Range(0, creaturesList.Count)], true);
        }
    }
    public void SpawnEdiblesRandomly(int count)
    {
        float edibleHeightOffset = levelManager.GetEdibleHeight();
        for (int i = 0; i < count; i++)
        {
            Vector3 randomCoords = GetRandomEdibleSpawnPoint(edibleHeightOffset);

            int plantIndex = Random.Range(0, ediblePrefabList.Count);
            //Generate the Prefab on the generated position
            Edible edible = Instantiate(ediblePrefabList[plantIndex], randomCoords, Quaternion.identity).GetComponent<Edible>();
            edible.name = "Plant" + i;
            edible.transform.parent = edibleParent;
        }
    }
    public void ActivateEdiblesRandomly(int numEdiblesToActivate)
    {
        float edibleHeightOffset = levelManager.GetEdibleHeight();

        List<Edible> ediblesToActivate = SpawnedEdibles.OrderBy(x => Guid.NewGuid()).Take(numEdiblesToActivate).ToList();
        foreach (Edible edible in ediblesToActivate)
        {
            edible.transform.position = new Vector3(edible.transform.position.x, Terrain.SampleHeight(edible.transform.position) + edibleHeightOffset, edible.transform.position.z);
            edible.Restart();
        }
    }
    public void ResetLevel()
    {
        ResetEdibles();
        ResetCreatures();
    }
    private void ResetCreatures()
    {
        ClearActiveAgents();

        SpawnPopulation(GetAllCreatures(), initialPopulation);

        ClearInactiveAgents();
        InactiveCreatures.Add(bestCreature); //else the will be garbage when the best creature is replaced        
    }

    private void ClearInactiveAgents()
    {
        foreach (Creature creature in InactiveCreatures.ToList())
        {
            if (creature != bestCreature)
                Destroy(creature.creatureHandler.gameObject);
        }
        InactiveCreatures.Clear();
    }

    private void ClearActiveAgents()
    {
        foreach (Creature creature in ActiveCreatures.ToList())
        {
            creature.creatureHandler.gameObject.SetActive(false);
        }
        ActiveCreatures.Clear();
    }

    private void ResetEdibles()
    {
        DeactivateAllEdibles();

        int numEdiblesToActivate = HandleDifficultyLevel();
        ActivateEdiblesRandomly(numEdiblesToActivate);

        //Debug.Log(string.Format("Spawned {0} edibles for avg fitness {1}.", numEdiblesToActivate, averageFitnessLastEpoch));
    }
    private void DestroyAllEdibles()
    {
        foreach (Edible edible in SpawnedEdibles)
            Destroy(edible.gameObject);
    }
    private void DestroyAllCreatures()
    {
        foreach (Transform creature in creatureParent)
            Destroy(creature.gameObject);
    }

    internal Vector3 GetRandomCreatureSpawnPoint()
    {
        Vector3 spawnPoint = creatureSpawnPoints[Random.Range(0, creatureSpawnPoints.Length)].position.MultiplyElementByElement(new Vector3(1,0,1)) + Random.insideUnitSphere.MultiplyElementByElement(new Vector3(1, 0, 1)) * Random.Range(-creatureSpawnRange, creatureSpawnRange) + Vector3.up * agentSpawnHeightOffset;
        spawnPoint = RestrictPositionOverTerrain(spawnPoint);
        return spawnPoint;
    }
    internal Vector3 GetRandomEdibleSpawnPoint(float yOffset = 0)
    {
        Vector3 spawnPoint = edibleSpawnPoints[Random.Range(0, edibleSpawnPoints.Length)].position + Random.insideUnitSphere.MultiplyElementByElement(new Vector3(1, 0, 1)) * Random.Range(-edibleSpawnRange, edibleSpawnRange);
        spawnPoint = RestrictPositionOverTerrain(spawnPoint);
        spawnPoint.y = Terrain.SampleHeight(spawnPoint) + yOffset;
        return spawnPoint;
    }

    private void DeactivateAllEdibles()
    {
        foreach (Edible edible in SpawnedEdibles)
            edible.gameObject.SetActive(false);
    }

    int HandleDifficultyLevel()
    {
        return (int)Mathf.Max((startingEdibles - averageFitnessLastEpoch / 2), targetEdibles);
    }
    public Vector3 GenerateRandomCoordinatesOnTerrain(float yOffset = 0.5f, float edgeOffset = 3)
    {
        float terrainWidth = Terrain.terrainData.size.x;
        float terrainLength = Terrain.terrainData.size.z;

        //Get terrain position
        float xTerrainPos = Terrain.transform.position.x;
        float zTerrainPos = Terrain.transform.position.z;

        //Generate random x,z,y position on the terrain
        float randX = Random.Range(xTerrainPos + edgeOffset, xTerrainPos + terrainWidth - edgeOffset);
        float randZ = Random.Range(zTerrainPos + edgeOffset, zTerrainPos + terrainLength - edgeOffset);
        float yVal = Terrain.SampleHeight(new Vector3(randX, 0, randZ));

        //Apply Offset if needed
        //yVal += yOffset;

        return new Vector3(randX, yVal + yOffset, randZ);
    }
    public Vector3 GenerateCoordinatesOnTerrainNearPosition(Vector3 globalPositionOnTerrain, float maxDistance = 5, float yOffset = 0.1f)
    {
        Vector3 randPoint = globalPositionOnTerrain + Random.insideUnitSphere.MultiplyElementByElement(new Vector3(1, 0, 1)) * maxDistance;
        randPoint = RestrictPositionOverTerrain(randPoint, 15);
        //Generate random x,z,y position on the terrain
        randPoint.y = Terrain.SampleHeight(new Vector3(randPoint.x, 0, randPoint.z)) + yOffset;

        return randPoint;
    }

    private Vector3 RestrictPositionOverTerrain(Vector3 randPoint, float edgeOffset = 5f)
    {
        float terrainWidth = Terrain.terrainData.size.x;
        float terrainLength = Terrain.terrainData.size.z;

        //Get terrain position
        float xTerrainPos = Terrain.transform.position.x;
        float zTerrainPos = Terrain.transform.position.z;
        float maxXPos = xTerrainPos + terrainWidth;
        float maxZPos = zTerrainPos + terrainLength;

        if (randPoint.x > maxXPos - edgeOffset)
            randPoint.x = maxXPos - edgeOffset;
        if (randPoint.z > maxZPos - edgeOffset)
            randPoint.z = maxZPos - edgeOffset;

        if (randPoint.x < xTerrainPos + edgeOffset)
            randPoint.x = xTerrainPos + edgeOffset;
        if (randPoint.z < zTerrainPos + edgeOffset)
            randPoint.z = zTerrainPos + edgeOffset;

        return randPoint;
    }

    public static string GenerateName(int nameLength)
    {
        System.Random r = new System.Random(Guid.NewGuid().GetHashCode());
        string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "t", "v", "w", "x" };
        string[] vowels = { "a", "e", "i", "o", "u", "y" };
        string name = "";
        name += consonants[r.Next(consonants.Length)].ToUpper();
        name += vowels[r.Next(vowels.Length)];
        int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
        while (b < nameLength)
        {
            name += consonants[r.Next(consonants.Length)];
            name += vowels[r.Next(vowels.Length)];
            b += 2;
        }

        return name.Substring(0, nameLength);
    }
    public Creature UpdateGetBestCreature()
    {
        return SortGetAllCreatures().First();
    }
    public Creature GetBestActiveCreature()
    {
        return Instance.levelManager.RankByFitness(ActiveCreatures).First();
    }

    #region Statistics    
    private void SaveCreatureAsPrefab(CreatureHandler creatureHandler, string prefabName = "BestCreature")
    {
#if UNITY_EDITOR
        // Set the path as within the Assets folder,
        // and name it as the GameObject's name with the .Prefab format
        string localPath = "Assets/Data/";
        Material mat = new Material(creatureHandler.creature.MainMeshRenderer.material);
        mat.color = creatureHandler.creature.creatureParameters.averageAgentColor;
        AssetDatabase.CreateAsset(mat, localPath + prefabName + "Mat.mat");
        //PrefabUtility.SaveAsPrefabAsset(mat, localPath + "SavedCreatureMat.mat");
        creatureHandler.creature.MainMeshRenderer.material = mat;
        foreach (BodyExtension body in creatureHandler.creature.creatureParameters.bodies)
        {
            body.meshRenderer.material = mat;
        }
        foreach (Limb limb in creatureHandler.creature.creatureParameters.limbs)
        {
            limb.meshRenderer.material = mat;
        }
        creatureHandler.creature.UpdateFitness();
        AssetDatabase.SaveAssets();
        // Make sure the file name is unique, in case an existing Prefab has the same name.
        //localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
        //Create the new Prefab.
        //AssetDatabase.DeleteAsset(localPath + prefabName + ".prefab");
        GameObject savedCreaturePrefab = PrefabUtility.SaveAsPrefabAsset(creatureHandler.gameObject, localPath + prefabName + ".prefab");
        savedCreaturePrefab.SetActive(true);
#endif
    }

    public IEnumerator HandleDataReporting()
    {
        float index = 0;
        while (true)
        {
            yield return new WaitForSeconds(dataRecordingInterval);
            ReportData(index);

            index += 1;
        }
    }

    public List<Creature> SortGetAllCreatures()
    {
        List<Creature> allCreatures = GetAllCreatures();
        if (allCreatures.Count == 0)
        {
            Debug.LogError("ERROR: No creatures in list provided!");
            return null;
        }
        allCreatures = Instance.levelManager.RankByFitness(allCreatures);

        return allCreatures;
    }

    private List<Creature> GetAllCreatures()
    {
        List<Creature> creatures = ActiveCreatures.Concat(InactiveCreatures).ToList();
        if (bestCreature && bestCreature.CreatureGeneration < Instance.currentEpoch)
        {
            creatures.Remove(bestCreature);
        }
        return creatures;
    }

    private void ReportData(float index)
    {
        List<Creature> allCreatures = SortGetAllCreatures();
        foreach (Creature creature in allCreatures)
            creature.RegisterExistingModules();

        float averageCreatureFitness = allCreatures.Sum(x => x.UpdateGetFitness()) / (float)allCreatures.Count;
        averageFitnessLastEpoch = averageCreatureFitness;
        float worstCreatureFitness = allCreatures.Last().UpdateGetFitness();
        Creature bestCreatureThisEpoch = allCreatures.First();
        float totalCreaturesWithLimbs = allCreatures.Where(x => x.creatureParameters.limbs.Count > 0).Sum(x => x.creatureParameters.limbs.Count);
        float totalCreaturesWithSensors = allCreatures.Where(x => x.creatureParameters.sensors.Count > 0).Sum(x => x.creatureParameters.sensors.Count);
        float averageCreatureViewDistance = allCreatures.Sum(x => x.GetAverageSensorViewDistance()) / (float)totalCreaturesWithSensors;
        float averageCreatureViewAngle = allCreatures.Sum(x => x.GetAverageSensorViewAngle()) / (float)totalCreaturesWithSensors;
        float averageCreatureLimbStrength = allCreatures.Sum(x => x.GetAverageLimbStrength()) / (float)totalCreaturesWithLimbs;
        float averageCreatureLimbCooldown = allCreatures.Sum(x => x.GetAverageLimbCooldown()) / (float)totalCreaturesWithLimbs;
        float averageCreatureSize = allCreatures.Sum(x => x.GetTotalSize()) / (float)allCreatures.Count;
        float averageCreatureLimbCount = allCreatures.Sum(x => x.creatureParameters.limbs.Count) / (float)allCreatures.Count;
        float averageCreatureSensorCount = allCreatures.Sum(x => x.creatureParameters.sensors.Count) / (float)allCreatures.Count;
        float averageCreatureColor = allCreatures.Sum(x => x.GetAverageCreatureColorIntensity()) / (float)allCreatures.Count;
        float averageCreatureMaxEnergy = allCreatures.Sum(x => x.creatureParameters.MaxEnergy) / (float)allCreatures.Count;
        float species = (speciesList == null ? 0 : speciesList.Count);
        float complexity = allCreatures.Sum(x => x.GetAgentComplexity()) / (float)allCreatures.Count;

        Helper.SaveFile(new FitnessData(index, worstCreatureFitness, bestCreatureThisEpoch, averageCreatureSensorCount, averageCreatureLimbCount, averageCreatureViewDistance,
             averageCreatureViewAngle, averageCreatureLimbStrength, averageCreatureLimbCooldown, averageCreatureSize, averageCreatureFitness, SpawnedEdibles.Where(x => x.gameObject.activeInHierarchy).ToList().Count, averageCreatureColor, averageCreatureMaxEnergy, species, complexity));

        if (bestCreatureThisEpoch.UpdateGetFitness() >= bestCreature.UpdateGetFitness())
        {
            SaveCreatureAsPrefab(UpdateGetBestCreature().creatureHandler, "BestCreature");
        }
    }

    private void UpdateBestCreature(List<Creature> sortedCreatureList)
    {
        Creature creatureCandidate = sortedCreatureList.First();
        if(bestCreature == null)
            bestCreature = creatureCandidate;
        else if (creatureCandidate.UpdateGetFitness() > bestCreature.UpdateGetFitness())
        {
            ActiveCreatures.Remove(bestCreature);
            InactiveCreatures.Remove(bestCreature);
            DestroyImmediate(bestCreature.creatureHandler.gameObject);
            bestCreature = creatureCandidate;
        }
    }
    #endregion
}

[System.Serializable]
public enum SimulationMode
{
    Free,
    Optimized
}
