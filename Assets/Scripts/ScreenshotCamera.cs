using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenshotCamera : MonoBehaviour
{
    public TextMeshPro descriptionText;
    public TextMeshPro nameText;

    public static string ScreenShotName(int epoch, string agentName)
    {
        return string.Format("{0}epoch_{1}_agent_{2}.png", Helper.pathToScreenshots, epoch, agentName);
    }

    public void TakeScreenshot(Creature creature, int resWidth = 960, int resHeight = 540, float distance = 5)
    {
        if (creature.lineage.Count > 0)
            descriptionText.text = "Lineage: " + creature.lineage.Last();
        else
            descriptionText.text = "Lineage: Progenitor";

        nameText.text = "Name: " + creature.name;
        creature.creatureHandler.transform.position += Vector3.up * 10;
        FocusOnCreature(creature, distance);
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        GetComponent<Camera>().targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        GetComponent<Camera>().Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        GetComponent<Camera>().targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = ScreenShotName(creature.CreatureGeneration, creature.name);
        System.IO.File.WriteAllBytes(filename, bytes);

        creature.creatureHandler.transform.position -= Vector3.up * 10;
    }

    private void FocusOnCreature(Creature creature, float distance)
    {
        this.transform.position = creature.transform.position + (creature.transform.forward * distance);
        this.transform.position += Vector3.up * distance/2;
        transform.LookAt(creature.transform.position);
    }
}
