using System.Security.Policy;

[System.Serializable]
public struct FitnessData
{
    public static string headers = "index\tAverage Performance\tBest Performance\tWorst Performance\tAverage Agent Brightness\tAverage Maximum Energy" +
        "\tAverage Sensors\tAverage Muscles\tAverage View Distance\tAverage View Angle\tAverage Muscle Strength\tAverage Muscle Frequency\tAverage Agent Size\tAverage Edibles\tAverage Species\tAverage Agent Complexity\tSimulation Index";
    public float minFitness;
    public Creature bestCreature;
    public float averageCreatureSensors;
    public float averageCreatureLimbs;
    public float averageCreatureViewDistance;
    public float averageCreatureViewAngle;
    public float averageCreatureLimbStrength;
    public float averageCreatureLimbFrequency;
    public float averageCreatureSize;
    public float averageCreatureFitness;
    public float spawnedEdiblesCount;
    public float averageCreatureColorIntensity;
    public float averageCreatureMaximumEnergy;
    public float index;
    public float species;
    public float averageComplexity;

    public FitnessData(float index, float minFitness, Creature bestCreature, float averageCreatureSensors, float averageCreatureLimbs, float averageCreatureViewDistance, float averageCreatureViewAngle, float averageCreatureLimbStrength, 
        float averageCreatureLimbCooldown, float averageCreatureSize, float averageCreatureFitness, float spawnedEdiblesCount, float averageCreatureColorIntensity, float averageCreatureMaximumEnergy, float species, float complexity)
    {
        this.index = index;
        this.minFitness = minFitness;
        this.bestCreature = bestCreature;
        this.averageCreatureSensors = averageCreatureSensors;
        this.averageCreatureLimbs = averageCreatureLimbs;
        this.averageCreatureViewDistance = averageCreatureViewDistance;
        this.averageCreatureViewAngle = averageCreatureViewAngle;
        this.averageCreatureLimbStrength = averageCreatureLimbStrength;
        this.averageCreatureLimbFrequency = averageCreatureLimbCooldown > 0 ? (1 / averageCreatureLimbCooldown) : 0;
        this.averageCreatureSize = averageCreatureSize;
        this.averageCreatureFitness = averageCreatureFitness;
        this.spawnedEdiblesCount = spawnedEdiblesCount;
        this.averageCreatureColorIntensity = averageCreatureColorIntensity;
        this.averageCreatureMaximumEnergy = averageCreatureMaximumEnergy;
        this.species = species;
        this.averageComplexity = complexity;
    }

    public string ToCsvFormat()
    {
        return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}", index, averageCreatureFitness,bestCreature.UpdateGetFitness(),minFitness, 
            averageCreatureColorIntensity, averageCreatureMaximumEnergy, averageCreatureSensors, averageCreatureLimbs,averageCreatureViewDistance, averageCreatureViewAngle, averageCreatureLimbStrength, 
            averageCreatureLimbFrequency, averageCreatureSize, spawnedEdiblesCount, species, averageComplexity, SimulationManager.Instance.simulationIndex);
    }
}