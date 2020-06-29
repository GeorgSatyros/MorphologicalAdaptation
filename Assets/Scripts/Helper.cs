using UnityEngine;
using System;
using System.IO;

public static class Helper
{
    public static int savingIndex = 0 ;
    public static string pathToStatFile = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\EvolutionThesis\Assets\Data\SavedStats\fitnessData.tsv";
    public static string pathToScreenshots = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\EvolutionThesis\Assets\Data\Screenshots\";

    public static T FindComponentInChildWithTag<T>(this GameObject parent, string tag) where T : Component
    {
        Transform t = parent.transform;
        foreach (Transform tr in t)
        {
            if (tr.CompareTag(tag))
            {
                return tr.GetComponent<T>();
            }
        }
        return null;
    }

    public static void DeleteStatsFile()
    {
        if (File.Exists(pathToStatFile))
            File.Delete(pathToStatFile);
    }

    public static void DeleteScreenshots()
    {
        if (Directory.Exists(pathToScreenshots))
        {
            DirectoryInfo directory = new DirectoryInfo(pathToScreenshots);
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }
        }
        else
            Directory.CreateDirectory(pathToScreenshots);
    }

    public static void SaveFile(FitnessData data)
    {
       
        if(File.Exists(pathToStatFile))
            using (StreamWriter file = new StreamWriter(pathToStatFile, true))
            {
                file.WriteLine(data.ToCsvFormat());
            }
        else
            using (StreamWriter file = new StreamWriter(pathToStatFile, true))
            {
                file.WriteLine(FitnessData.headers);
                file.WriteLine(data.ToCsvFormat());
            }
    }

}

public static class Vector3Extensions
{
    public static Vector3 DivideElementByElement(this Vector3 vectorA, Vector3 vectorB)
    {
        return new Vector3(vectorA.x/vectorB.x, vectorA.y / vectorB.y, vectorA.z / vectorB.z);
    }

    public static Vector3 MultiplyElementByElement(this Vector3 vectorA, Vector3 vectorB)
    {
        return new Vector3(vectorA.x * vectorB.x, vectorA.y * vectorB.y, vectorA.z * vectorB.z);
    }
}