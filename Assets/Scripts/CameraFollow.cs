using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class CameraFollow : MonoBehaviour
{
    public Transform currentTarget;
    public float cameraDistance = 30;
    private int indexer = 0;

    void LateUpdate()
    {
        if (SimulationManager.Instance.ActiveCreatures.Count <= 0)
            return;
        else if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {          
            currentTarget = SimulationManager.Instance.GetBestActiveCreature().transform;
        }

        if (Input.GetKeyDown(KeyCode.Space))
            if (SimulationManager.Instance.ActiveCreatures.Count > indexer+1)
            {
                indexer++;
                currentTarget = SimulationManager.Instance.ActiveCreatures[indexer].transform;
            }
            else
            { 
                indexer = 0;
                currentTarget = SimulationManager.Instance.ActiveCreatures[indexer].transform;
            }        
        if (Input.GetKeyDown(KeyCode.Return))
            currentTarget = SimulationManager.Instance.GetBestActiveCreature().transform;

        if (currentTarget)
        {
            Vector3 newPos = currentTarget.position - (Vector3.forward * cameraDistance);
            newPos.y += cameraDistance;
            float blend = 1f - Mathf.Pow(1f - 0.1f, Time.deltaTime * 30f);
            transform.position = Vector3.Lerp(this.transform.position, newPos, blend);
            transform.LookAt(currentTarget.position);
        }

    }
    private void Update()
    {
        cameraDistance += -Input.mouseScrollDelta.y/3;
        this.transform.position += Vector3.right * Input.GetAxis("Horizontal") * 0.2f;
    }

}