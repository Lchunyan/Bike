using System.Collections;
using System.Collections.Generic;
using SBPScripts;
using TMPro;
using UnityEngine;

public class GetDeviceName : MonoBehaviour
{
    TextMeshPro Meshtext;
    void Start()
    {
        Meshtext  = GetComponent<TextMeshPro>();

        Invoke("GetName",5);
    }


    void GetName()
    {
        BicycleController bc = transform.parent.GetComponent<BicycleController>();
        string name = bc.DeviceName;
        Meshtext.text = name;
    }
}
