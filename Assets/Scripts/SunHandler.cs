using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class SunHandler : MonoBehaviour
{
    
    [Tooltip("The heat emitted. 0 is perfect conditions.")]
    [Range(-10,10)]
    public float heatIntensity = 0;

    public void Awake()
    {
        if(heatIntensity > 0)
            GetComponent<Light>().color = Color.Lerp(Color.white,Color.red, Math.Abs(heatIntensity) / 10);
        else
            GetComponent<Light>().color = Color.Lerp(Color.white, new Color(0.61f,0.61f,1,1), Math.Abs(heatIntensity) / 10);
    }
}
