using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbAnimationHandler : MonoBehaviour
{
    private Limb thisLimb;

    private Transform root;
    private Transform limbRootR;
    private Transform topLimbR;
    //private Transform upperLimbR;
    //private Transform kneeR;
    //private Transform tipR;    
    //private Transform limbRootL;
    //private Transform topLimbL;
    //private Transform upperLimbL;
    //private Transform kneeL;
    //private Transform tipL;

    bool isDoingAnimation = false;
    Quaternion startingRotationR;
    //Quaternion startingRotationL; 
    

    void Start()
    {
        thisLimb = this.GetComponent<Limb>();
        SetupLimbSkeleton();
    }

    private void SetupLimbSkeleton()
    {
        root = this.transform;
        limbRootR = root.GetChild(0);
        topLimbR = limbRootR.GetChild(0);
        //upperLimbR = topLimbR.GetChild(0);
        //kneeR = upperLimbR.GetChild(0);
        //tipR = kneeR.GetChild(0);

        //limbRootL = transform.GetChild(1);
        //topLimbL = limbRootL.GetChild(0);
        //upperLimbL = topLimbL.GetChild(0);
        //kneeL = upperLimbL.GetChild(0);
        //tipL = kneeL.GetChild(0);
    }

    private void FixedUpdate()
    {
        if (isDoingAnimation)
        {
            topLimbR.Rotate(Vector3.right, -180 * Time.fixedDeltaTime);
            //topLimbL.Rotate(Vector3.right, -180 * Time.deltaTime);
        }
    }

    public void DoWalkAnimation(float inSeconds)
    {
        if (isDoingAnimation)
            return;

        startingRotationR = topLimbR.localRotation;
        //startingRotationL = topLimbL.localRotation;

        isDoingAnimation = true;
        if(gameObject.activeInHierarchy)
            StartCoroutine(StopAnimationInSeconds(inSeconds));
    }

    IEnumerator StopAnimationInSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isDoingAnimation = false;
        topLimbR.localRotation = startingRotationR;
        //topLimbL.localRotation = startingRotationL;
    }
}
