using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OtterController : MonoBehaviour
{
    [Header("Legs")]
    [SerializeField] OtterFrontArmSwimmer frontLeftLegStepper;
    [SerializeField] OtterFrontArmSwimmer frontRightLegStepper;
    public void Awake()
    {
        Debug.Log("Hello, Unity Awaked!");
        StartCoroutine(LegUpdateCoroutine());
    }

    IEnumerator LegUpdateCoroutine()
    {
        while (true)
        {
            frontLeftLegStepper.TrySwing();
            frontRightLegStepper.TrySwing();
            yield return null;
        }
    }
    }